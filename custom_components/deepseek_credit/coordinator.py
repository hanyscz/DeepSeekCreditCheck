"""DataUpdateCoordinator for DeepSeek Credit Checker integration."""
from __future__ import annotations

from datetime import datetime, timedelta
from typing import Any
import aiohttp

from homeassistant.core import HomeAssistant
from homeassistant.helpers.update_coordinator import DataUpdateCoordinator, UpdateFailed

from .const import DOMAIN, CONF_API_KEY, CONF_SESSION_TOKEN, LOGGER

def get_safe_node(data: Any, *keys: str) -> Any:
    """Safely traverse a nested dictionary/list structure."""
    if data is None:
        return None
    current = data
    for key in keys:
        if isinstance(current, dict) and key in current:
            current = current[key]
        elif isinstance(current, list):
            try:
                idx = int(key)
                if 0 <= idx < len(current):
                    current = current[idx]
                else:
                    return None
            except ValueError:
                return None
        else:
            return None
    return current

def parse_usage_amount(node: Any) -> tuple[int, int, int]:
    """Recursively parse token usage amounts (CacheHit, CacheMiss, Response)."""
    cache_hit = 0
    cache_miss = 0
    response = 0

    if node is None:
        return cache_hit, cache_miss, response

    if isinstance(node, dict):
        if "type" in node and ("amount" in node or "value" in node or "count" in node):
            node_type = str(node.get("type", ""))
            val_str = str(node.get("amount") or node.get("value") or node.get("count") or "0")
            try:
                val = int(float(val_str))
                if node_type.upper() == "PROMPT_CACHE_HIT_TOKEN":
                    cache_hit += val
                elif node_type.upper() == "PROMPT_CACHE_MISS_TOKEN":
                    cache_miss += val
                elif node_type.upper() == "RESPONSE_TOKEN":
                    response += val
                elif node_type.upper() == "PROMPT_TOKEN" and val > 0:
                    cache_miss += val
            except ValueError:
                pass

        for val in node.values():
            sub_hit, sub_miss, sub_resp = parse_usage_amount(val)
            cache_hit += sub_hit
            cache_miss += sub_miss
            response += sub_resp

    elif isinstance(node, list):
        for item in node:
            sub_hit, sub_miss, sub_resp = parse_usage_amount(item)
            cache_hit += sub_hit
            cache_miss += sub_miss
            response += sub_resp

    return cache_hit, cache_miss, response

def parse_usage_cost(node: Any) -> float:
    """Recursively parse usage costs."""
    if node is None:
        return 0.0
    cost_sum = 0.0

    if isinstance(node, dict):
        if "type" in node and ("cost" in node or "amount" in node or "value" in node):
            node_type = str(node.get("type", ""))
            val_str = node.get("cost") or node.get("amount") or node.get("value")
            if node_type.upper() == "REQUEST":
                val_str = None
            
            if val_str is not None:
                try:
                    cost_sum += float(val_str)
                except ValueError:
                    pass

        for val in node.values():
            cost_sum += parse_usage_cost(val)

    elif isinstance(node, list):
        for item in node:
            cost_sum += parse_usage_cost(item)

    return cost_sum

class DeepSeekCreditCoordinator(DataUpdateCoordinator[dict[str, Any]]):
    """Class to manage fetching DeepSeek credit and usage data."""

    def __init__(
        self,
        hass: HomeAssistant,
        session: aiohttp.ClientSession,
        api_key: str | None,
        session_token: str | None,
    ) -> None:
        """Initialize the coordinator."""
        self.session = session
        self.api_key = api_key
        self.session_token = session_token

        super().__init__(
            hass,
            LOGGER,
            name=DOMAIN,
            update_interval=timedelta(minutes=15),
        )

    async def _async_update_data(self) -> dict[str, Any]:
        """Fetch data from DeepSeek APIs."""
        data: dict[str, Any] = {}

        # 1. Fetch balance via API Key if configured
        if self.api_key:
            try:
                headers = {
                    "Authorization": f"Bearer {self.api_key}",
                    "Accept": "application/json",
                }
                async with self.session.get(
                    "https://api.deepseek.com/user/balance",
                    headers=headers,
                    timeout=15,
                ) as response:
                    if response.status != 200:
                        raise UpdateFailed(f"Error fetching balance: HTTP {response.status}")
                    
                    balance_data = await response.json()
                    self._parse_balance(balance_data, data)
            except Exception as err:
                LOGGER.error("Error fetching balance: %s", err)
                if not self.session_token:
                    raise UpdateFailed(f"Failed to fetch balance: {err}") from err

        # 2. Fetch usage via Session Token if configured
        if self.session_token:
            try:
                token = self.session_token
                if not token.startswith("Bearer "):
                    token = f"Bearer {self.session_token}"
                
                headers = {
                    "Authorization": token,
                    "Accept": "application/json",
                    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                }

                now = datetime.now()
                year = now.year
                month = now.month

                amount_url = f"https://platform.deepseek.com/api/v0/usage/amount?year={year}&month={month}"
                cost_url = f"https://platform.deepseek.com/api/v0/usage/cost?year={year}&month={month}"

                amount_json = None
                cost_json = None

                async with self.session.get(amount_url, headers=headers, timeout=15) as amt_resp:
                    if amt_resp.status == 200:
                        amount_json = await amt_resp.json()
                        if amount_json and amount_json.get("code") != 0:
                            LOGGER.error("DeepSeek usage amount error: %s", amount_json.get("msg"))
                            amount_json = None
                    else:
                        LOGGER.error("Failed to fetch usage amount: HTTP %s", amt_resp.status)

                async with self.session.get(cost_url, headers=headers, timeout=15) as cost_resp:
                    if cost_resp.status == 200:
                        cost_json = await cost_resp.json()
                        if cost_json and cost_json.get("code") != 0:
                            LOGGER.error("DeepSeek usage cost error: %s", cost_json.get("msg"))
                            cost_json = None
                    else:
                        LOGGER.error("Failed to fetch usage cost: HTTP %s", cost_resp.status)

                if amount_json or cost_json:
                    self._parse_usage(amount_json, cost_json, data)
            except Exception as err:
                LOGGER.error("Error fetching usage stats: %s", err)
                if not self.api_key:
                    raise UpdateFailed(f"Failed to fetch usage: {err}") from err

        return data

    def _parse_balance(self, balance_data: dict[str, Any], data: dict[str, Any]) -> None:
        """Parse balance data and update target dict."""
        balance_infos = balance_data.get("balance_infos", [])
        for info in balance_infos:
            currency = str(info.get("currency", "USD")).upper()
            try:
                total_val = float(info.get("total_balance", "0.00"))
                topped_up_val = float(info.get("topped_up_balance", "0.00"))
                granted_val = float(info.get("granted_balance", "0.00"))
            except ValueError:
                total_val = topped_up_val = granted_val = 0.0

            data[f"balance_{currency.lower()}_total"] = total_val
            data[f"balance_{currency.lower()}_topped_up"] = topped_up_val
            data[f"balance_{currency.lower()}_granted"] = granted_val

    def _parse_usage(self, amount_json: Any, cost_json: Any, data: dict[str, Any]) -> None:
        """Parse month and daily usage (tokens & costs)."""
        # --- Celkové měsíční statistiky (Amount) ---
        pro_m_tokens = 0
        flash_m_tokens = 0

        amount_total_node = (
            get_safe_node(amount_json, "data", "biz_data", "total")
            or get_safe_node(amount_json, "data", "total")
            or get_safe_node(amount_json, "total")
            or amount_json
        )

        if isinstance(amount_total_node, list):
            for item in amount_total_node:
                if isinstance(item, dict):
                    model_name = str(item.get("model", ""))
                    usage_node = item.get("usage")
                    hit, miss, resp = parse_usage_amount(usage_node)
                    tot = hit + miss + resp
                    if "flash" in model_name.lower():
                        flash_m_tokens += tot
                    else:
                        pro_m_tokens += tot
        elif amount_total_node:
            hit, miss, resp = parse_usage_amount(amount_total_node)
            pro_m_tokens = hit + miss + resp

        # --- Celkové měsíční statistiky (Cost) ---
        pro_m_cost = 0.0
        flash_m_cost = 0.0

        cost_total_node = (
            get_safe_node(cost_json, "data", "biz_data", 0, "total")
            or get_safe_node(cost_json, "data", "biz_data", "total")
            or get_safe_node(cost_json, "data", "total")
            or get_safe_node(cost_json, "total")
            or cost_json
        )

        if isinstance(cost_total_node, list):
            for item in cost_total_node:
                if isinstance(item, dict):
                    model_name = str(item.get("model", ""))
                    usage_node = item.get("usage")
                    cost_val = parse_usage_cost(usage_node)
                    if "flash" in model_name.lower():
                        flash_m_cost += cost_val
                    else:
                        pro_m_cost += cost_val
        elif cost_total_node:
            pro_m_cost = parse_usage_cost(cost_total_node)

        # Uložení měsíčních hodnot
        data["monthly_tokens_pro"] = pro_m_tokens
        data["monthly_tokens_flash"] = flash_m_tokens
        data["monthly_tokens_total"] = pro_m_tokens + flash_m_tokens
        data["monthly_cost_pro"] = pro_m_cost
        data["monthly_cost_flash"] = flash_m_cost
        data["monthly_cost_total"] = pro_m_cost + flash_m_cost

        # --- Denní statistiky (Dnešek) ---
        today_str = datetime.today().strftime("%Y-%m-%d")
        
        # Denní tokeny (Amount)
        pro_d_tokens = 0
        flash_d_tokens = 0

        amount_days_node = (
            get_safe_node(amount_json, "data", "biz_data", "days")
            or get_safe_node(amount_json, "data", "days")
            or get_safe_node(amount_json, "days")
        )

        if isinstance(amount_days_node, list):
            today_node = next((x for x in amount_days_node if isinstance(x, dict) and x.get("date") == today_str), None)
            if today_node:
                today_data = today_node.get("data") or today_node.get("usage_amount")
                if isinstance(today_data, list):
                    for item in today_data:
                        if isinstance(item, dict):
                            model_name = str(item.get("model", ""))
                            usage_node = item.get("usage")
                            hit, miss, resp = parse_usage_amount(usage_node)
                            tot = hit + miss + resp
                            if "flash" in model_name.lower():
                                flash_d_tokens += tot
                            else:
                                pro_d_tokens += tot

        # Denní náklady (Cost)
        pro_d_cost = 0.0
        flash_d_cost = 0.0

        cost_days_node = (
            get_safe_node(cost_json, "data", "biz_data", 0, "days")
            or get_safe_node(cost_json, "data", "biz_data", "days")
            or get_safe_node(cost_json, "data", "days")
            or get_safe_node(cost_json, "days")
        )

        if isinstance(cost_days_node, list):
            today_cost_node = next((x for x in cost_days_node if isinstance(x, dict) and x.get("date") == today_str), None)
            if today_cost_node:
                today_cost_data = today_cost_node.get("data") or today_cost_node.get("usage_cost")
                if isinstance(today_cost_data, list):
                    for item in today_cost_data:
                        if isinstance(item, dict):
                            model_name = str(item.get("model", ""))
                            usage_node = item.get("usage")
                            cost_val = parse_usage_cost(usage_node)
                            if "flash" in model_name.lower():
                                flash_d_cost += cost_val
                            else:
                                pro_d_cost += cost_val

        # Uložení denních hodnot
        data["daily_tokens_pro"] = pro_d_tokens
        data["daily_tokens_flash"] = flash_d_tokens
        data["daily_tokens_total"] = pro_d_tokens + flash_d_tokens
        data["daily_cost_pro"] = pro_d_cost
        data["daily_cost_flash"] = flash_d_cost
        data["daily_cost_total"] = pro_d_cost + flash_d_cost
