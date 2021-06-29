using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TypeHierarchyViewer.Views
{
    /// <summary>
    /// 型階層を表示する View です。
    /// </summary>
    public partial class TypeHierarchyView : UserControl
    {
        /// <summary>
        /// インスタンスを初期化します。
        /// </summary>
        public TypeHierarchyView()
        {
            InitializeComponent();
            TypeTree.ContextMenu = TypeTree.Resources["TypeItemMenu"] as System.Windows.Controls.ContextMenu;
        }

        /// <summary>
        /// 選択した項目の定義を開きます。
        /// </summary>
        private void Item_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as TreeViewItem;
            if (item?.IsSelected ?? false)
            {
                e.Handled = true;

                GotoDefinition(item.DataContext as TypeNode);
            }
        }

        private void ContextMenu_GotoDefinition(object sender, RoutedEventArgs e)
        {
            if (TypeTree.ContextMenu == null)
                return;
            var typeNode = TypeTree.SelectedItem as TypeNode;
            if (typeNode != null)
            {
                e.Handled = true;

                GotoDefinition(typeNode);
            }
        }

        private void ContextMenu_ViewTypeHierachy(object sender, RoutedEventArgs e)
        {
            if (TypeTree.ContextMenu == null)
                return;
            var typeNode = TypeTree.SelectedItem as TypeNode;
            if (typeNode != null)
            {
                e.Handled = true;
                OpenTypeHierarchyCommand.Instance.ShowToolWindowAsync(typeNode.Source);
            }
        }

        private void GotoDefinition(TypeNode typeNode)
        {
            if (typeNode == null)
                return;
            var viewModel = (TypeHierarchyViewModel)DataContext;
            viewModel.OpenSymbol(typeNode);
        }

        private void RightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as DependencyObject) as TreeViewItem;

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                treeViewItem.IsSelected = true;
                e.Handled = true;
            }
        }

        static DependencyObject VisualUpwardSearch<T>(DependencyObject source)
        {
            while (source != null && source.GetType() != typeof(T))
                source = VisualTreeHelper.GetParent(source);
            return source;
        }
    }
}