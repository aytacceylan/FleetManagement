using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
			// Eski helper bağlantısını temizle
			if (combo.Tag is IDisposable oldState)
			{
				oldState.Dispose();
			}

			combo.DisplayMemberPath = displayMember;
			combo.SelectedValuePath = selectedValueMember;

			combo.IsEditable = true;
			combo.IsReadOnly = false;
			combo.IsTextSearchEnabled = false;
			combo.StaysOpenOnEdit = true;

			// Her ComboBox kendi kaynak listesini kullanır.
			var items = source.ToList();

			combo.ItemsSource = items;

			var view = CollectionViewSource.GetDefaultView(items);

			var state = new ComboBoxSearchState<T>(
				combo,
				view,
				searchSelector);

			combo.Tag = state;

			state.Attach();
		}

		private sealed class ComboBoxSearchState<T> : IDisposable
		{
			private readonly ComboBox _combo;
			private readonly ICollectionView _view;
			private readonly Func<T, string> _searchSelector;

			private readonly TextChangedEventHandler _textChanged;
			private readonly EventHandler _dropDownOpened;
			private readonly EventHandler _dropDownClosed;

            private bool _isFiltering;

            private bool _disposed;

			public ComboBoxSearchState(
				ComboBox combo,
				ICollectionView view,
				Func<T, string> searchSelector)
			{
				_combo = combo;
				_view = view;
				_searchSelector = searchSelector;

				_textChanged = OnTextChanged;
				_dropDownOpened = OnDropDownOpened;
				_dropDownClosed = OnDropDownClosed;
			}

			public void Attach()
			{
				_combo.AddHandler(
					TextBoxBase.TextChangedEvent,
					_textChanged);

				_combo.DropDownOpened += _dropDownOpened;
				_combo.DropDownClosed += _dropDownClosed;
			}

			private void OnDropDownOpened(
				object? sender,
				EventArgs e)
			{
				if (_disposed)
					return;

				// ComboBox her açıldığında eski aramayı kaldır.
				// Böylece önceki seçilen sürücü yüzünden
				// liste tek kişide kalmaz.
				_view.Filter = null;
				_view.Refresh();
			}

			private void OnDropDownClosed(
				object? sender,
				EventArgs e)
			{
				if (_disposed)
					return;

				// Kullanıcı bir seçim yaptıysa dropdown kapanır.
				// Seçimin oluşturduğu TextChanged olayının
				// listeyi kalıcı olarak filtrelemesine izin verme.
				_view.Filter = null;
				_view.Refresh();
			}

            private void OnTextChanged(
    object sender,
    TextChangedEventArgs e)
            {
                if (_disposed || _isFiltering)
                    return;

                // Kullanıcı ComboBox üzerinde işlem yapmıyorsa
                // programatik seçim değişikliğini arama olarak değerlendirme.
                if (!_combo.IsKeyboardFocusWithin)
                    return;


                if (_combo.Template.FindName(
                    "PART_EditableTextBox",
                    _combo) is not TextBox textBox)
                    return;

                // Kullanıcının yazdığı metin
                var searchText = textBox.Text ?? "";
                var text = searchText.Trim();

                // Kullanıcı yazarken dropdown otomatik açılsın.
                if (!_combo.IsDropDownOpen &&
                    _combo.IsKeyboardFocusWithin &&
                    !string.IsNullOrWhiteSpace(text))
                {
                    _combo.IsDropDownOpen = true;
                }

                try
                {
                    _isFiltering = true;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        _view.Filter = null;
                    }
                    else
                    {
                        _view.Filter = item =>
                        {
                            if (item is not T value)
                                return false;

                            var searchValue = _searchSelector(value) ?? "";

                            return searchValue.IndexOf(
                                text,
                                StringComparison.CurrentCultureIgnoreCase) >= 0;
                        };
                    }

                    _view.Refresh();

                    // Filtreleme sırasında WPF metni değiştirmişse geri yükle.
                    if (textBox.Text != searchText)
                        textBox.Text = searchText;

                    textBox.SelectionStart = textBox.Text.Length;
                    textBox.SelectionLength = 0;
                }
                finally
                {
                    _isFiltering = false;
                }
            }

            public void Dispose()
			{
				if (_disposed)
					return;

				_disposed = true;

				_combo.RemoveHandler(
					TextBoxBase.TextChangedEvent,
					_textChanged);

				_combo.DropDownOpened -= _dropDownOpened;
				_combo.DropDownClosed -= _dropDownClosed;

				_view.Filter = null;
				_view.Refresh();
			}
		}
	}
}