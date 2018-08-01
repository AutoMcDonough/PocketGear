using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

// ReSharper disable UsePatternMatching

namespace AutoMcD.PocketGear {
    public static class TerminalControlUtils {
        // todo: finalize this class and move it to mod utils project.
        public static IMyTerminalControlListbox CreaListbox<TBlock>(string id, string title, Action<TBlock, List<MyTerminalControlListBoxItem>, List<MyTerminalControlListBoxItem>> content, Action<TBlock, List<MyTerminalControlListBoxItem>> selected, bool multiselect, int visibleRowsCount, string tooltip = "", Func<TBlock, bool> enabled = null, Func<TBlock, bool> visible = null, bool supportsMultipleBlocks = false) where TBlock : IMyTerminalBlock {
            var listbox = CreateControl<IMyTerminalControlListbox, TBlock>(id, title, tooltip, enabled, visible, supportsMultipleBlocks);

            listbox.Multiselect = multiselect;
            listbox.VisibleRowsCount = visibleRowsCount;
            listbox.ListContent = (block, list, sel) => content((TBlock) block, list, sel);
            listbox.ItemSelected = (block, list) => selected((TBlock) block, list);

            return listbox;
        }

        public static IMyTerminalControlButton CreateButton<TBlock>(string id, string title, Action<TBlock> action, string tooltip = "", Func<TBlock, bool> enabled = null, Func<TBlock, bool> visible = null, bool supportsMultipleBlocks = false) where TBlock : IMyTerminalBlock {
            var button = CreateControl<IMyTerminalControlButton, TBlock>(id, title, tooltip, enabled, visible, supportsMultipleBlocks);
            button.Action = block => action((TBlock) block);

            return button;
        }

        public static IMyTerminalControlCheckbox CreateCheckbox<TBlock>(string id, string title, Func<TBlock, bool> getter, Action<TBlock, bool> setter, string tooltip = "", Func<TBlock, bool> enabled = null, Func<TBlock, bool> visible = null, bool supportsMultipleBlocks = false) where TBlock : IMyTerminalBlock {
            var checkbox = CreateControl<IMyTerminalControlCheckbox, TBlock>(id, title, tooltip, enabled, visible, supportsMultipleBlocks);

            checkbox.Getter = block => getter((TBlock) block);
            checkbox.Setter = (block, value) => setter((TBlock) block, value);

            return checkbox;
        }

        public static IMyTerminalControlColor CreateColor<TBlock>(string id, string title, Func<TBlock, Color> getter, Action<TBlock, Color> setter, string tooltip = "", Func<TBlock, bool> enabled = null, Func<TBlock, bool> visible = null, bool supportsMultipleBlocks = false) where TBlock : IMyTerminalBlock {
            var color = CreateControl<IMyTerminalControlColor, TBlock>(id, title, tooltip, enabled, visible, supportsMultipleBlocks);

            color.Getter = block => getter((TBlock) block);
            color.Setter = (block, value) => setter((TBlock) block, value);
            return color;
        }

        public static IMyTerminalControlCombobox CreateCombobox<TBlock>(string id, string title, Action<List<MyTerminalControlComboBoxItem>> content, Func<TBlock, long> getter, Action<TBlock, long> setter, string tooltip = "", Func<TBlock, bool> enabled = null, Func<TBlock, bool> visible = null, bool supportsMultipleBlocks = false) where TBlock : IMyTerminalBlock {
            var combobox = CreateControl<IMyTerminalControlCombobox, TBlock>(id, title, tooltip, enabled, visible, supportsMultipleBlocks);

            combobox.ComboBoxContent = content;
            combobox.Getter = block => getter((TBlock) block);
            combobox.Setter = (block, value) => setter((TBlock) block, value);

            return combobox;
        }

        public static IMyTerminalControlLabel CreateLabel<TBlock>(string id, string title, Func<TBlock, bool> enabled = null, Func<TBlock, bool> visible = null, bool supportsMultipleBlocks = false) where TBlock : IMyTerminalBlock {
            var label = CreateControl<IMyTerminalControlLabel, TBlock>(id, title, null, enabled, visible, supportsMultipleBlocks);

            label.Label = MyStringId.GetOrCompute(title);

            return label;
        }

        public static IMyTerminalControlOnOffSwitch CreateOnOffSwitch<TBlock>(string id, string title, Func<TBlock, bool> getter, Action<TBlock, bool> setter, string tooltip = "", Func<TBlock, bool> enabled = null, Func<TBlock, bool> visible = null, bool supportsMultipleBlocks = false, string onText = "On", string offText = "Off") where TBlock : IMyTerminalBlock {
            var @switch = CreateControl<IMyTerminalControlOnOffSwitch, TBlock>(id, title, tooltip, enabled, visible, supportsMultipleBlocks);

            @switch.OnText = MyStringId.GetOrCompute(onText);
            @switch.OffText = MyStringId.GetOrCompute(offText);
            @switch.Getter = block => getter((TBlock) block);
            @switch.Setter = (block, value) => setter((TBlock) block, value);

            return @switch;
        }

        public static IMyTerminalControlSeparator CreateSeparator<TBlock>(Func<TBlock, bool> enabled = null, Func<TBlock, bool> visible = null, bool supportsMultipleBlocks = false) where TBlock : IMyTerminalBlock {
            var seperator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, TBlock>(string.Empty);
            seperator.Enabled = block => enabled?.Invoke((TBlock) block) ?? true;
            seperator.Visible = block => visible?.Invoke((TBlock) block) ?? true;
            seperator.SupportsMultipleBlocks = supportsMultipleBlocks;
            return seperator;
        }

        public static IMyTerminalControlSlider CreateSlider<TBlock>(string id, string title, Action<TBlock, StringBuilder> writer, Func<TBlock, float> getter, Action<TBlock, float> setter, Func<IMyTerminalBlock, float> min, Func<IMyTerminalBlock, float> max, string tooltip = "", Func<TBlock, bool> enabled = null, Func<TBlock, bool> visible = null, bool supportsMultipleBlocks = false) where TBlock : IMyTerminalBlock {
            var slider = CreateControl<IMyTerminalControlSlider, TBlock>(id, title, tooltip, enabled, visible, supportsMultipleBlocks);

            slider.Writer = (block, builder) => writer((TBlock) block, builder);
            slider.Getter = block => getter((TBlock) block);
            slider.Setter = (block, value) => setter((TBlock) block, value);
            slider.SetLimits(min, max);

            return slider;
        }

        public static void RegisterControls<TBlock>(List<IMyTerminalControl> controls) where TBlock : IMyTerminalBlock {
            foreach (var control in controls) {
                var checkbox = control as IMyTerminalControlCheckbox;
                if (checkbox != null) {
                    var actions = CreateOnOffActions<IMyTerminalControlCheckbox, TBlock>(checkbox);
                    var property = CreateProperty<bool, TBlock>(checkbox);

                    MyAPIGateway.TerminalControls.AddControl<TBlock>(checkbox);
                    MyAPIGateway.TerminalControls.AddControl<TBlock>(property);
                    foreach (var action in actions) {
                        MyAPIGateway.TerminalControls.AddAction<TBlock>(action);
                    }

                    continue;
                }

                var combobox = control as IMyTerminalControlCombobox;
                if (combobox != null) {
                    var property = CreateProperty<long, TBlock>(combobox);

                    MyAPIGateway.TerminalControls.AddControl<TBlock>(combobox);
                    MyAPIGateway.TerminalControls.AddControl<TBlock>(property);
                    continue;
                }

                var @switch = control as IMyTerminalControlOnOffSwitch;
                if (@switch != null) {
                    var actions = CreateOnOffActions<IMyTerminalControlOnOffSwitch, TBlock>(@switch);
                    var property = CreateProperty<bool, TBlock>(@switch);

                    MyAPIGateway.TerminalControls.AddControl<TBlock>(@switch);
                    MyAPIGateway.TerminalControls.AddControl<TBlock>(property);
                    foreach (var action in actions) {
                        MyAPIGateway.TerminalControls.AddAction<TBlock>(action);
                    }

                    continue;
                }

                // todo add other control types.
                MyAPIGateway.TerminalControls.AddControl<TBlock>(control);
            }
        }

        private static TControl CreateControl<TControl, TBlock>(string id, string title, string tooltip, Func<TBlock, bool> enabled, Func<TBlock, bool> visible, bool supportsMultipleBlocks) where TControl : IMyTerminalControl where TBlock : IMyTerminalBlock {
            if (string.IsNullOrWhiteSpace(id)) {
                throw new ArgumentNullException(nameof(id), "Title can't be null or whitespaces");
            }

            var control = MyAPIGateway.TerminalControls.CreateControl<TControl, TBlock>(id);
            if (control is IMyTerminalControlTitleTooltip) {
                var titleTooltipControl = control as IMyTerminalControlTitleTooltip;
                titleTooltipControl.Title = MyStringId.GetOrCompute(title);
                titleTooltipControl.Tooltip = MyStringId.GetOrCompute(tooltip);
            }

            control.Enabled = block => enabled?.Invoke((TBlock) block) ?? false;
            control.Visible = block => visible?.Invoke((TBlock) block) ?? false;
            control.SupportsMultipleBlocks = supportsMultipleBlocks;

            return control;
        }

        private static List<IMyTerminalAction> CreateOnOffActions<TControl, TBlock>(TControl control) where TBlock : IMyTerminalBlock where TControl : IMyTerminalControl, IMyTerminalValueControl<bool>, IMyTerminalControlTitleTooltip {
            var actions = new List<IMyTerminalAction>();
            var id = ((IMyTerminalControl) control).Id;
            Action<IMyTerminalBlock, StringBuilder> writer;
            StringBuilder switchName;
            StringBuilder onName;
            StringBuilder offName;
            var @switch = control as IMyTerminalControlOnOffSwitch;
            if (@switch != null) {
                switchName = new StringBuilder($"{@switch.OnText}/{@switch.OffText}");
                onName = new StringBuilder($"{@switch.OnText}");
                offName = new StringBuilder($"{@switch.OffText}");
                writer = (block, builder) => builder.Append(control.Getter.Invoke(block) ? @switch.OnText : @switch.OffText);
            } else {
                switchName = new StringBuilder($"{control.Title} {MySpaceTexts.ON}/{MySpaceTexts.OFF}");
                onName = new StringBuilder($"{control.Title} {MySpaceTexts.ON}");
                offName = new StringBuilder($"{control.Title} {MySpaceTexts.OFF}");
                writer = (block, builder) => builder.Append(control.Getter.Invoke(block) ? $"{MySpaceTexts.ON}" : $"{MySpaceTexts.OFF}");
            }

            var onOffAction = MyAPIGateway.TerminalControls.CreateAction<TBlock>($"{id}_OnOff");
            onOffAction.Name = switchName;
            onOffAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            onOffAction.Writer = writer;
            onOffAction.Enabled = control.Enabled;
            onOffAction.Action = block => control.Setter(block, !control.Getter(block));
            onOffAction.ValidForGroups = control.SupportsMultipleBlocks;
            actions.Add(onOffAction);

            var onAction = MyAPIGateway.TerminalControls.CreateAction<TBlock>($"{id}_On");
            onAction.Name = onName;
            onAction.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            onAction.Writer = writer;
            onAction.Enabled = control.Enabled;
            onAction.Action = block => control.Setter(block, true);
            onAction.ValidForGroups = control.SupportsMultipleBlocks;
            actions.Add(onAction);

            var offAction = MyAPIGateway.TerminalControls.CreateAction<TBlock>($"{id}_Off");
            offAction.Name = offName;
            offAction.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
            offAction.Writer = writer;
            offAction.Enabled = control.Enabled;
            offAction.Action = block => control.Setter(block, false);
            offAction.ValidForGroups = control.SupportsMultipleBlocks;
            actions.Add(offAction);

            return actions;
        }

        private static IMyTerminalControlProperty<T> CreateProperty<T, TBlock>(IMyTerminalValueControl<T> control) where TBlock : IMyTerminalBlock {
            var property = MyAPIGateway.TerminalControls.CreateProperty<T, TBlock>(control.Id);
            property.SupportsMultipleBlocks = false;
            property.Getter = control.Getter;
            property.Setter = control.Setter;
            return property;
        }

        private static string PascalCase(string s) {
            if (string.IsNullOrWhiteSpace(s)) {
                return s;
            }

            if (s.Length < 2) {
                return s.ToUpper();
            }

            var words = s.Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);

            return words.Aggregate("", (current, word) => current + word.Substring(0, 1).ToUpper() + word.Substring(1));
        }
    }
}