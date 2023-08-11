﻿using HASS.Agent.Commands;
using HASS.Agent.MQTT;
using HASS.Agent.Resources.Localization;
using HASS.Agent.Sensors;
using HASS.Agent.Settings;
using HASS.Agent.Shared;
using HASS.Agent.Shared.Functions;
using HASS.Agent.Shared.Models.Config;
using HASS.Agent.Shared.Models.HomeAssistant;
using Octokit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace HASS.Agent.Compatibility
{
    internal class NameCompatibilityTask : ICompatibilityTask
    {
        public string Name => Languages.Compat_NameTask_Name;

        private (List<ConfiguredSensor>, List<ConfiguredSensor>) ConvertSingleValueSensors(List<AbstractSingleValueSensor> sensors)
        {
            var configuredSensors = new List<ConfiguredSensor>();
            var toBeDeletedSensors = new List<ConfiguredSensor>();

            foreach (var sensor in sensors)
            {
                var currentConfiguredSensor = StoredSensors.ConvertAbstractSingleValueToConfigured(sensor);

                if (!sensor.Name.Contains(SharedHelperFunctions.GetSafeConfiguredDeviceName()))
                {
                    configuredSensors.Add(currentConfiguredSensor);
                    continue;
                }

                var newName = sensor.Name.Replace($"{SharedHelperFunctions.GetSafeConfiguredDeviceName()}_", "");
                var objectId = $"{SharedHelperFunctions.GetSafeConfiguredDeviceName()}_{newName}";
                if (objectId == sensor.Name)
                {
                    var newConfiguredSensor = StoredSensors.ConvertAbstractSingleValueToConfigured(sensor);
                    newConfiguredSensor.Name = newName;
                    configuredSensors.Add(newConfiguredSensor);

                    toBeDeletedSensors.Add(currentConfiguredSensor);
                }
                else
                {
                    configuredSensors.Add(currentConfiguredSensor);
                }
            }

            return (configuredSensors, toBeDeletedSensors);
        }

        private (List<ConfiguredSensor>, List<ConfiguredSensor>) ConvertMultiValueSensors(List<AbstractMultiValueSensor> sensors)
        {
            var configuredSensors = new List<ConfiguredSensor>();
            var toBeDeletedSensors = new List<ConfiguredSensor>();

            foreach (var sensor in sensors)
            {
                var currentConfiguredSensor = StoredSensors.ConvertAbstractMultiValueToConfigured(sensor);

                if (!sensor.Name.Contains(SharedHelperFunctions.GetSafeConfiguredDeviceName()))
                {
                    configuredSensors.Add(currentConfiguredSensor);
                    continue;
                }

                var newName = sensor.Name.Replace($"{SharedHelperFunctions.GetSafeConfiguredDeviceName()}_", "");
                var objectId = $"{SharedHelperFunctions.GetSafeConfiguredDeviceName()}_{newName}";
                if (objectId == sensor.Name)
                {
                    var newConfiguredSensor = StoredSensors.ConvertAbstractMultiValueToConfigured(sensor);
                    newConfiguredSensor.Name = newName;
                    configuredSensors.Add(newConfiguredSensor);

                    toBeDeletedSensors.Add(currentConfiguredSensor);
                }
                else
                {
                    configuredSensors.Add(currentConfiguredSensor);
                }
            }

            return (configuredSensors, toBeDeletedSensors);
        }

        private (List<ConfiguredCommand>, List<ConfiguredCommand>) ConvertCommands(List<AbstractCommand> commands)
        {
            var configuredCommands = new List<ConfiguredCommand>();
            var toBeDeletedCommands = new List<ConfiguredCommand>();

            foreach (var command in commands)
            {
                var currentConfiguredCommand = StoredCommands.ConvertAbstractToConfigured(command);

                if (!command.Name.Contains(SharedHelperFunctions.GetSafeConfiguredDeviceName()))
                {
                    configuredCommands.Add(currentConfiguredCommand);
                    continue;
                }

                var newName = command.Name.Replace($"{SharedHelperFunctions.GetSafeConfiguredDeviceName()}_", "");
                var objectId = $"{SharedHelperFunctions.GetSafeConfiguredDeviceName()}_{newName}";
                if (objectId == command.Name)
                {
                    var newConfiguredCommand = StoredCommands.ConvertAbstractToConfigured(command);
                    newConfiguredCommand.Name = newName;
                    configuredCommands.Add(newConfiguredCommand);

                    toBeDeletedCommands.Add(currentConfiguredCommand);
                }
                else
                {
                    configuredCommands.Add(currentConfiguredCommand);
                }
            }

            return (configuredCommands, toBeDeletedCommands);
        }

        public async Task<(bool, string)> Perform()
        {
            try
            {
                var errorMessage = string.Empty;

                Log.Information("[COMPATTASK] Sensor name compatibility task started");

                AgentSharedBase.Initialize(Variables.AppSettings.DeviceName, Variables.MqttManager, Variables.AppSettings.CustomExecutorBinary);

                await SettingsManager.LoadEntitiesAsync();
                Variables.MqttManager.Initialize();

                while (!Variables.MqttManager.IsConnected())
                    await Task.Delay(1000);

                SensorsManager.Initialize();
                SensorsManager.Pause();
                CommandsManager.Initialize();

                Log.Information("[COMPATTASK] Modifying stored single value sensors");
                var (sensors, toBeDeletedSensors) = ConvertSingleValueSensors(Variables.SingleValueSensors);
                await SensorsManager.StoreAsync(sensors, toBeDeletedSensors);
                SensorsManager.Pause();

                Log.Information("[COMPATTASK] Modifying stored multi value sensors");
                (sensors, toBeDeletedSensors) = ConvertMultiValueSensors(Variables.MultiValueSensors);
                await SensorsManager.StoreAsync(sensors, toBeDeletedSensors);
                SensorsManager.Pause();

                Log.Information("[COMPATTASK] Modifying stored commands");
                var (commands, toBeDeletedCommands) = ConvertCommands(Variables.Commands);
                await CommandsManager.StoreAsync(commands, toBeDeletedCommands);
                CommandsManager.Pause();

                Log.Information("[COMPATTASK] Sensor name compatibility task ended");

                return string.IsNullOrWhiteSpace(errorMessage) ? (true, string.Empty) : (false, errorMessage);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "[COMPATTASK] Error performing sensor name compatibility task: {err}", ex.Message);
                return (false, Languages.Compat_Error_CheckLogs);
            }
        }
    }
}