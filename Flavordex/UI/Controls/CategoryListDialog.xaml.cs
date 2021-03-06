﻿/*
  The MIT License (MIT)
  Copyright © 2016 Steve Guidetti

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the “Software”), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in
  all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
  THE SOFTWARE.
*/
using Flavordex.Models.Data;
using Flavordex.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Flavordex.UI.Controls
{
    /// <summary>
    /// Dialog to add, edit, and delete journal categories.
    /// </summary>
    public sealed partial class CategoryListDialog : ContentDialog
    {
        /// <summary>
        /// Gets the list of Categories to display.
        /// </summary>
        private ObservableCollection<CategoryItemViewModel> Categories =
            new ObservableCollection<CategoryItemViewModel>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public CategoryListDialog()
        {
            InitializeComponent();
            LoadCategories();
        }

        /// <summary>
        /// Loads the list of Categories.
        /// </summary>
        private async void LoadCategories()
        {
            var categories = new Collection<CategoryItemViewModel>();
            foreach (var category in await DatabaseHelper.GetCategoryListAsync())
            {
                categories.Add(new CategoryItemViewModel(category));
            }
            foreach (var category in categories.OrderBy(e => e.Name))
            {
                Categories.Add(category);
            }
        }

        /// <summary>
        /// Navigates to the Edit Category Page when a list item is clicked.
        /// </summary>
        /// <param name="sender">The ListView.</param>
        /// <param name="e">The event arguments.</param>
        private void OnItemClicked(object sender, ItemClickEventArgs e)
        {
            Hide();

            var categoryId = (e.ClickedItem as CategoryItemViewModel).Model.ID;
            (Window.Current.Content as Frame).Navigate(typeof(EditCategoryPage), categoryId);
        }

        /// <summary>
        /// Deletes a Category after user confirmation when a delete button is clicked.
        /// </summary>
        /// <param name="sender">The Button.</param>
        /// <param name="e">The event arguments.</param>
        private async void OnDeleteCategory(object sender, RoutedEventArgs e)
        {
            var category = (sender as FrameworkElement).DataContext as CategoryItemViewModel;
            if (category.IsPreset)
            {
                return;
            }

            Hide();

            var result = await new DeleteCategoryDialog(category.Model).ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await DatabaseHelper.DeleteCategoryAsync(category.Model);
                Categories.Remove(category);
            }

            await ShowAsync();
        }

        /// <summary>
        /// Navigates to the Edit Category Page to create a new Category when the Create Category
        /// footer Button is clicked.
        /// </summary>
        /// <param name="sender">The Button.</param>
        /// <param name="e">The event arguments.</param>
        private void OnAddCategory(object sender, RoutedEventArgs e)
        {
            Hide();
            (Window.Current.Content as Frame).Navigate(typeof(EditCategoryPage));
        }
    }
}
