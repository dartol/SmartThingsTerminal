﻿using Newtonsoft.Json;
using SmartThingsNet.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace SmartThingsTerminal.Scenarios
{
    [ScenarioMetadata(Name: "Rules", Description: "SmartThings rules")]
    [ScenarioCategory("Rules")]
    class Rules : Scenario
    {
        public override void Setup()
        {
            Dictionary<string, dynamic> displayItemList = null;
            try
            {
                if (STClient.GetAllRules().Items?.Count > 0)
                {
                    displayItemList = STClient.GetAllRules().Items
                        .GroupBy(r => r.Name)
                        .Select(g => g.First())
                        .Select(t => new KeyValuePair<string, dynamic>(t.Name, t))
                        .ToDictionary(t => t.Key, t => t.Value);
                }
            }
            catch (SmartThingsNet.Client.ApiException exp)
            {
                SetErrorView($"Error calling API: {exp.Source} {exp.ErrorCode} {exp.Message}");
            }
            catch (System.Exception exp)
            {
                SetErrorView($"Unknown error calling API: {exp.Message}");
            }
            ConfigureWindows<Rule>(displayItemList);
        }

        public override void ConfigureStatusBar()
        {
            StatusBar = new StatusBar(new StatusItem[] {
                new StatusItem(Key.F3, "~F3~ Edit", () => EnableEditMode()),
                new StatusItem(Key.F4, "~F4~ Save", () => SaveItem()),
                new StatusItem(Key.F5, "~F5~ Refresh Data", () => RefreshScreen()),
                new StatusItem(Key.F6, "~F6~ Copy Rule", () => SaveItem(true)),
                new StatusItem(Key.F9, "~F9~ Delete Rule", () => DeleteItem()),
                new StatusItem(Key.Home, "~Home~ Back", () => Quit())
            });
        }

        public bool SaveItem(bool copyCurrent = false)
        {
            var json = JsonView?.Text.ToString();

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var rule = JsonConvert.DeserializeObject<Rule>(json);
                    RuleRequest ruleRequest = new RuleRequest(rule.Name, rule.Actions, rule.TimeZoneId);

                    string locationId = GetRuleLocation(rule);
                    if (copyCurrent)
                    {
                        int nameCounter = STClient.GetAllRules().Items.Where(n => n.Name.Equals(rule.Name)).Count();
                        nameCounter++;
                        ruleRequest.Name += $"-copy {nameCounter}";
                        STClient.CreateRule(locationId, ruleRequest);
                    }
                    else
                    {
                        var response = STClient.UpdateRule(rule.Id, locationId, ruleRequest);
                    }
                    RefreshScreen();
                    ShowStatusBarMessage($"Rule: updated");
                }
                catch (System.Exception exp)
                {
                    ShowStatusBarMessage($"Error updating: {exp.Message}");
                }
            }
            return true;
        }

        public override void DeleteItem()
        {
            if (SelectedItem != null)
            {
                Rule currentRule = (Rule)SelectedItem;
                try
                {
                    STClient.DeleteRule(currentRule.Id, GetRuleLocation(currentRule));
                    base.DeleteItem();
                    RefreshScreen();
                }
                catch (Exception exp)
                {
                    ShowStatusBarMessage($"Error deleting: {exp.Message}");
                }
            }
        }

        private string GetRuleLocation(Rule rule)
        {
            // Get the locationId for this rule
            string locationId = null;
            foreach (var location in STClient.GetAllLocations().Items)
            {
                var locationRules = STClient.GetAllRules(location.LocationId.ToString()).Items.Where(r => r.Id == rule.Id);
                locationId = location.LocationId.ToString();
                break;
            }

            return locationId;
        }
    }
}