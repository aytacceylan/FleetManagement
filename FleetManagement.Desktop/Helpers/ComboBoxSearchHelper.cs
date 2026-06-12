using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace FleetManagement.Desktop.Helpers
{
    public static class ComboBoxSearchHelper
    {
        public static void BindContains<T>(
            ComboBox combo,
            List<T> source,
            string displayMember,
            string selectedValueMember,
            Func<T, string> searchSelector)
        {
            combo.DisplayMemberPath = displayMember;
            combo.SelectedValuePath = selectedValueMember;

            combo.ItemsSource = source;

            combo.IsEditable = true;
            combo.IsReadOnly = false;
            combo.IsTextSearchEnabled = false;
            combo.StaysOpenOnEdit = true;

            ICollectionView view =
                CollectionViewSource.GetDefaultView(combo.ItemsSource);

            combo.AddHandler(
                TextBoxBase.TextChangedEvent,
                new TextChangedEventHandler((s, e) =>
                {
                    string text = combo.Text;

                    if (!combo.IsDropDownOpen)
                        combo.IsDropDownOpen = true;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        view.Filter = null;
                    }
                    else
                    {
                        view.Filter = x =>
                        {
                            var value = searchSelector((T)x);

                            if (string.IsNullOrEmpty(value))
                                return false;

                            return value.IndexOf(
                                text,
                                StringComparison.OrdinalIgnoreCase) >= 0;
                        };
                    }

                    view.Refresh();

                    if (combo.Template.FindName(
                        "PART_EditableTextBox",
                        combo) is TextBox tb)
                    {
                        tb.SelectionStart = tb.Text.Length;
                    }
                }));
        }
    }
}