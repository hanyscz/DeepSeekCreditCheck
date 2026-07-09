"""Config flow for DeepSeek Credit Checker integration."""
from __future__ import annotations

from typing import Any
import voluptuous as vol
import aiohttp

from homeassistant import config_entries
from homeassistant.core import HomeAssistant
from homeassistant.data_entry_flow import FlowResult
from homeassistant.helpers.aiohttp_client import async_get_clientsession

from .const import DOMAIN, CONF_API_KEY, CONF_SESSION_TOKEN, LOGGER

DATA_SCHEMA = vol.Schema(
    {
        vol.Optional(CONF_API_KEY): str,
        vol.Optional(CONF_SESSION_TOKEN): str,
    }
)

async def validate_api_key(session: aiohttp.ClientSession, api_key: str) -> bool:
    """Validate the API key by requesting the balance."""
    try:
        headers = {
            "Authorization": f"Bearer {api_key}",
            "Accept": "application/json",
        }
        async with session.get("https://api.deepseek.com/user/balance", headers=headers, timeout=10) as response:
            if response.status == 200:
                res_data = await response.json()
                # Zkontrolujeme, zda odpověď obsahuje klíčové vlastnosti
                if "balance_infos" in res_data or "is_available" in res_data:
                    return True
            LOGGER.warning("API key validation failed with status code: %s", response.status)
    except Exception as err:
        LOGGER.error("API key validation exception: %s", err)
    return False

async def validate_session_token(session: aiohttp.ClientSession, session_token: str) -> bool:
    """Validate the session token by requesting user summary."""
    try:
        token = session_token
        if not token.startswith("Bearer "):
            token = f"Bearer {session_token}"
        
        headers = {
            "Authorization": token,
            "Accept": "application/json",
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        }
        async with session.get("https://platform.deepseek.com/api/v0/users/get_user_summary", headers=headers, timeout=10) as response:
            if response.status == 200:
                res_data = await response.json()
                if res_data and res_data.get("code") == 0:
                    return True
            LOGGER.warning("Session token validation failed with status code: %s", response.status)
    except Exception as err:
        LOGGER.error("Session token validation exception: %s", err)
    return False

class DeepSeekCreditConfigFlow(config_entries.ConfigFlow, domain=DOMAIN):
    """Handle a config flow for DeepSeek Credit Checker."""

    VERSION = 1

    async def async_step_user(
        self, user_input: dict[str, Any] | None = None
    ) -> FlowResult:
        """Handle the initial step."""
        errors: dict[str, str] = {}

        if user_input is not None:
            api_key = user_input.get(CONF_API_KEY, "").strip()
            session_token = user_input.get(CONF_SESSION_TOKEN, "").strip()

            if not api_key and not session_token:
                errors["base"] = "missing_credentials"
            else:
                session = async_get_clientsession(self.hass)

                # Validace zadaných klíčů
                if api_key:
                    api_valid = await validate_api_key(session, api_key)
                    if not api_valid:
                        errors[CONF_API_KEY] = "invalid_api_key"

                if session_token:
                    token_valid = await validate_session_token(session, session_token)
                    if not token_valid:
                        errors[CONF_SESSION_TOKEN] = "invalid_session_token"

                if not errors:
                    # Vyčistíme prázdné nebo bílé znaky
                    cleaned_input = {}
                    if api_key:
                        cleaned_input[CONF_API_KEY] = api_key
                    if session_token:
                        cleaned_input[CONF_SESSION_TOKEN] = session_token

                    # Ujistíme se, že nevytvoříme duplicitní záznam
                    await self.async_set_unique_id(DOMAIN)
                    self._abort_if_unique_id_configured()

                    return self.async_create_entry(
                        title="DeepSeek Account", data=cleaned_input
                    )

        return self.async_show_form(
            step_id="user",
            data_schema=DATA_SCHEMA,
            errors=errors,
        )
