using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using XWall.Properties;

namespace XWall {
    /// <summary>
    /// Interaction logic for CustomRulesEditor.xaml
    /// </summary>
    public partial class CustomRulesEditor : Window {
        static Settings settings = Settings.Default;
        static ResourceDictionary resources = App.Current.Resources;
        string lastCopied;
        Regex urlRe = new Regex(@"(?:[a-z0-9-]+://)?((?:(?:[a-z0-9-]+\.)+[a-z]{2,}|\d{1,3}(?:\.\d{1,3}){3})(?::\d+)?)(?:[/\s]|$)", RegexOptions.IgnoreCase);
        Regex domainRe = new Regex(@"^(?:[a-z0-9-]+\.)+[a-z]{2,}$");
        Regex ruleRe = new Regex(@"^(?:(?:www\.)?([a-z0-9-.\*\?]+))?((?::\d+)?(?:/(.*))?)$", RegexOptions.IgnoreCase);

        public CustomRulesEditor() {
            InitializeComponent();
            InitializeBinding();

            //context menu
            customRulesListBox.ContextMenu = new ContextMenu();
            var deleteMenuItem = new MenuItem();
            deleteMenuItem.Header = resources["DeleteSelected"] as string;
            deleteMenuItem.Click += (sender, e) => {
                var rules = Rules.CustomRules.Rules;
                var items = new List<object>();
                foreach (var item in customRulesListBox.SelectedItems)
                    items.Add(item);
                foreach (var item in items)
                    rules.Remove(item as string);
            };
            customRulesListBox.ContextMenu.Items.Add(deleteMenuItem);

            deleteMenuItem.IsEnabled = false;

            customRulesListBox.SelectionChanged += (sender, e) => {
                deleteMenuItem.IsEnabled = customRulesListBox.SelectedItems.Count > 0;
            };
        }

        private void onWindowActivated(object sender, EventArgs e) {
            var copied = Clipboard.GetText();

            if (!String.IsNullOrEmpty(copied) && copied != lastCopied) {
                lastCopied = copied;
                var match = urlRe.Match(copied);
                if (match.Success)
                    newRuleTextBox.Text = match.Groups[1].Value;
            }

            newRuleTextBox.SelectAll();
            newRuleTextBox.Focus();
        }

        private void onAddClick(object sender, RoutedEventArgs e) {
            addNewRule();
        }

        private void onClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            e.Cancel = true;
            Hide();
        }

        private void onNewRuleTextBoxKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter)
                addNewRule();
        }

        private void addNewRule() {
            var rule = newRuleTextBox.Text.Trim();
            var withExc = rule.StartsWith("!");
            if (withExc) {
                rule = rule.Substring(1);
            }
            
            if (rule == "" && !withExc) {
                MessageBox.Show(resources["EnterRuleMessage"] as string, resources["CustomRulesEditorTitle"] as string, MessageBoxButton.OK, MessageBoxImage.Warning);
                newRuleTextBox.Text = "";
                newRuleTextBox.Focus();
                return;
            }

            var match = ruleRe.Match(rule);

            if (rule == "" || !match.Success) {
                MessageBox.Show(resources["InvalidRuleMessage"] as string, resources["CustomRulesEditorTitle"] as string, MessageBoxButton.OK, MessageBoxImage.Error);
                newRuleTextBox.SelectAll();
                newRuleTextBox.Focus();
                return;
            }
            else {
                var pathReStr = match.Groups[3].Value;

                try {
                    new Regex(pathReStr);
                }
                catch {
                    MessageBox.Show(resources["InvalidRulePathMessage"] as string, resources["CustomRulesEditorTitle"] as string, MessageBoxButton.OK, MessageBoxImage.Error);
                    newRuleTextBox.SelectAll();
                    newRuleTextBox.Focus();
                    return;
                }
            }

            if (settings.CustomRulesAddSubdomains) {
                var domain = match.Groups[1].Value.ToLower();
                var other = match.Groups[2].Value;

                if (String.IsNullOrEmpty(other) && domainRe.IsMatch(domain))
                    rule = "." + domain;
            }

            if (withExc)
                rule = "!" + rule;

            var result = Rules.CustomRules.Add(rule);

            if (!result) {
                MessageBox.Show(resources["RuleExistsMessage"] as string, resources["CustomRulesEditorTitle"] as string, MessageBoxButton.OK, MessageBoxImage.Warning);
                newRuleTextBox.SelectAll();
                newRuleTextBox.Focus();
                return;
            }

            newRuleTextBox.Text = "";

            if (customRulesExpander.IsExpanded) {
                var lastIndex = customRulesListBox.Items.Count - 1;
                customRulesListBox.SelectedIndex = lastIndex;
                customRulesListBox.ScrollIntoView(customRulesListBox.SelectedItem);
            }
            else Hide();
        }

        private void onWindowLoaded(object sender, RoutedEventArgs e) {
            customRulesListBox.ItemsSource = Rules.CustomRules.Rules;
        }
    }
}
