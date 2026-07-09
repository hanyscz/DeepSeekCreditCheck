"""Sensor platform for DeepSeek Credit Checker integration."""
from __future__ import annotations

from typing import Any
from homeassistant.components.sensor import (
    SensorDeviceClass,
    SensorEntity,
    SensorStateClass,
)
from homeassistant.config_entries import ConfigEntry
from homeassistant.core import HomeAssistant
from homeassistant.helpers.entity import DeviceInfo
from homeassistant.helpers.entity_platform import AddEntitiesCallback
from homeassistant.helpers.update_coordinator import CoordinatorEntity

from .const import DOMAIN
from .coordinator import DeepSeekCreditCoordinator

async def async_setup_entry(
    hass: HomeAssistant,
    entry: ConfigEntry,
    async_add_entities: AddEntitiesCallback,
) -> None:
    """Set up DeepSeek Credit Checker sensors based on config entry."""
    coordinator: DeepSeekCreditCoordinator = hass.data[DOMAIN][entry.entry_id]
    entities: list[SensorEntity] = []

    # Device Info shared by all entities
    device_info = DeviceInfo(
        identifiers={(DOMAIN, entry.entry_id)},
        name="DeepSeek Account",
        manufacturer="DeepSeek",
        model="API Platform",
    )

    # 1. Add Balance Sensors (USD and/or CNY) if API Key is configured
    if coordinator.api_key:
        if "balance_usd_total" in coordinator.data:
            entities.append(
                DeepSeekBalanceSensor(
                    coordinator, "usd", "USD", device_info, entry.entry_id
                )
            )
        if "balance_cny_total" in coordinator.data:
            entities.append(
                DeepSeekBalanceSensor(
                    coordinator, "cny", "CNY", device_info, entry.entry_id
                )
            )

    # 2. Add Usage Sensors if Session Token is configured
    if coordinator.session_token:
        # Costs
        entities.append(
            DeepSeekUsageCostSensor(
                coordinator,
                "monthly_cost_total",
                "Monthly Cost",
                "monthly_cost",
                device_info,
                entry.entry_id,
            )
        )
        entities.append(
            DeepSeekUsageCostSensor(
                coordinator,
                "daily_cost_total",
                "Daily Cost",
                "daily_cost",
                device_info,
                entry.entry_id,
            )
        )
        # Tokens
        entities.append(
            DeepSeekUsageTokensSensor(
                coordinator,
                "monthly_tokens_total",
                "Monthly Tokens",
                "monthly_tokens",
                device_info,
                entry.entry_id,
            )
        )
        entities.append(
            DeepSeekUsageTokensSensor(
                coordinator,
                "daily_tokens_total",
                "Daily Tokens",
                "daily_tokens",
                device_info,
                entry.entry_id,
            )
        )

    async_add_entities(entities)

class DeepSeekBaseSensor(CoordinatorEntity[DeepSeekCreditCoordinator], SensorEntity):
    """Base class for DeepSeek Checker sensors."""

    def __init__(
        self,
        coordinator: DeepSeekCreditCoordinator,
        unique_suffix: str,
        name: str,
        device_info: DeviceInfo,
        entry_id: str,
    ) -> None:
        """Initialize the sensor."""
        super().__init__(coordinator)
        self._attr_unique_id = f"{entry_id}_{unique_suffix}"
        self._attr_name = f"DeepSeek {name}"
        self._attr_device_info = device_info

class DeepSeekBalanceSensor(DeepSeekBaseSensor):
    """Sensor for DeepSeek API balance."""

    _attr_device_class = SensorDeviceClass.MONETARY
    _attr_state_class = SensorStateClass.MEASUREMENT

    def __init__(
        self,
        coordinator: DeepSeekCreditCoordinator,
        currency_key: str,
        currency_unit: str,
        device_info: DeviceInfo,
        entry_id: str,
    ) -> None:
        """Initialize the balance sensor."""
        super().__init__(
            coordinator,
            f"balance_{currency_key}",
            f"Balance {currency_unit}",
            device_info,
            entry_id,
        )
        self._currency_key = currency_key
        self._attr_native_unit_of_measurement = currency_unit

    @property
    def native_value(self) -> float | None:
        """Return the state of the sensor."""
        return self.coordinator.data.get(f"balance_{self._currency_key}_total")

    @property
    def extra_state_attributes(self) -> dict[str, Any]:
        """Return entity specific state attributes."""
        return {
            "topped_up_balance": self.coordinator.data.get(
                f"balance_{self._currency_key}_topped_up", 0.0
            ),
            "granted_balance": self.coordinator.data.get(
                f"balance_{self._currency_key}_granted", 0.0
            ),
        }

class DeepSeekUsageCostSensor(DeepSeekBaseSensor):
    """Sensor for DeepSeek usage cost."""

    _attr_device_class = SensorDeviceClass.MONETARY
    _attr_state_class = SensorStateClass.MEASUREMENT
    _attr_native_unit_of_measurement = "USD"

    def __init__(
        self,
        coordinator: DeepSeekCreditCoordinator,
        data_key: str,
        name: str,
        unique_suffix: str,
        device_info: DeviceInfo,
        entry_id: str,
    ) -> None:
        """Initialize the cost sensor."""
        super().__init__(
            coordinator,
            unique_suffix,
            name,
            device_info,
            entry_id,
        )
        self._data_key = data_key
        self._is_monthly = "monthly" in unique_suffix

    @property
    def native_value(self) -> float | None:
        """Return the state of the sensor."""
        return self.coordinator.data.get(self._data_key)

    @property
    def extra_state_attributes(self) -> dict[str, Any]:
        """Return entity specific state attributes."""
        prefix = "monthly_cost" if self._is_monthly else "daily_cost"
        return {
            "pro_cost": self.coordinator.data.get(f"{prefix}_pro", 0.0),
            "flash_cost": self.coordinator.data.get(f"{prefix}_flash", 0.0),
        }

class DeepSeekUsageTokensSensor(DeepSeekBaseSensor):
    """Sensor for DeepSeek usage tokens count."""

    _attr_state_class = SensorStateClass.MEASUREMENT
    _attr_native_unit_of_measurement = "tokens"
    _attr_icon = "mdi:counter"

    def __init__(
        self,
        coordinator: DeepSeekCreditCoordinator,
        data_key: str,
        name: str,
        unique_suffix: str,
        device_info: DeviceInfo,
        entry_id: str,
    ) -> None:
        """Initialize the tokens sensor."""
        super().__init__(
            coordinator,
            unique_suffix,
            name,
            device_info,
            entry_id,
        )
        self._data_key = data_key
        self._is_monthly = "monthly" in unique_suffix

    @property
    def native_value(self) -> int | None:
        """Return the state of the sensor."""
        return self.coordinator.data.get(self._data_key)

    @property
    def extra_state_attributes(self) -> dict[str, Any]:
        """Return entity specific state attributes."""
        prefix = "monthly_tokens" if self._is_monthly else "daily_tokens"
        return {
            "pro_tokens": self.coordinator.data.get(f"{prefix}_pro", 0),
            "flash_tokens": self.coordinator.data.get(f"{prefix}_flash", 0),
        }
