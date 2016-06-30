﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.VisualStudio.LanguageServices;

namespace TypeHierarchyViewer.Views
{
    /// <summary>
    /// <see cref="TypeHierarchyView"/>の ViewModel です。
    /// </summary>
    public class TypeHierarchyViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// 現在のワークスペースです。
        /// </summary>
        private VisualStudioWorkspace _workspace;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        private INamedTypeSymbol _targetType;
        /// <summary>
        /// 型階層のターゲットを取得します。
        /// </summary>
        public INamedTypeSymbol TargetType
        {
            get { return _targetType; }
            private set
            {
                if (_targetType != value)
                {
                    _targetType = value;
                    OnPropertyChanged(nameof(TargetType));
                }
            }
        }

        private TypeNode[] _typeNodes;
        /// <summary>
        /// 型階層のノードを取得します。
        /// </summary>
        public TypeNode[] TypeNodes
        {
            get { return _typeNodes; }
            private set
            {
                if (_typeNodes != value)
                {
                    _typeNodes = value;
                    OnPropertyChanged(nameof(TypeNodes));
                }
            }
        }

        /// <summary>
        /// 型階層をクリアします。
        /// </summary>
        public void Clear()
        {
            TargetType = null;
            TypeNodes = new TypeNode[0];
        }

        /// <summary>
        /// 型階層を初期化します。
        /// </summary>
        /// <param name="targetType">対象の型</param>
        /// <param name="workspace">現在のワークスペース</param>
        public void InitializeTargetType(INamedTypeSymbol targetType, VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
            TargetType = targetType;
            TypeNodes = CreateTypeNodes(targetType);
        }

        /// <summary>
        /// 指定されたノードの定義を開きます。
        /// </summary>
        public void OpenSymbol(TypeNode node)
        {
            if (node == null)
            {
                return;
            }

            foreach (var project in _workspace.CurrentSolution.Projects)
            {
                if (_workspace.TryGoToDefinition(node.Source, project, CancellationToken.None))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// <see cref="PropertyChangedEventHandler"/>イベントを発生させます。
        /// </summary>
        /// <param name="propertyName">変更されたプロパティ名</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 型階層のノードを作成します。
        /// </summary>
        private TypeNode[] CreateTypeNodes(INamedTypeSymbol targetType)
        {
            if (targetType == null)
            {
                return new TypeNode[0];
            }

            TypeNode topNode;
            if (targetType.TypeKind == TypeKind.Interface)
            {
                topNode = new TypeNode(targetType) { IsBaseNode = true };
                topNode.Children = SymbolFinder.FindImplementationsAsync(targetType, _workspace.CurrentSolution).Result
                    .OfType<INamedTypeSymbol>()
                    .Where(x => x.Locations.Any(y => y.IsInSource))
                    .Select(x => new TypeNode(x))
                    .ToArray();
            }
            else
            {
                var baseTypes = GetBaseTypes(targetType);
                topNode = CreateTopNode(targetType, baseTypes);
            }

            return new[] { topNode }
                .Concat(targetType.AllInterfaces
                    .Select(x => new TypeNode(x)))
                .ToArray();
        }

        /// <summary>
        /// 型階層の最上位ノードを作成します。
        /// </summary>
        private TypeNode CreateTopNode(INamedTypeSymbol targetType, Stack<INamedTypeSymbol> baseTypes)
        {
            if (baseTypes.Count == 0)
            {
                // MEMO : object の場合
                return new TypeNode(targetType);
            }

            var result = new TypeNode(baseTypes.Pop());

            var current = result;
            foreach (var type in baseTypes)
            {
                var child = new TypeNode(type);
                current.Children = new[] { child };
                current = child;
            }

            var leafNode = new TypeNode(targetType) { IsBaseNode = true };
            leafNode.Children = SymbolFinder.FindDerivedClassesAsync(targetType, _workspace.CurrentSolution).Result
                .Where(x => x.Locations.Any(y => y.IsInSource))
                .Select(x => new TypeNode(x))
                .ToArray();

            current.Children = new[] { leafNode };

            return result;
        }

        /// <summary>
        /// 親クラスの一覧を最上位から順に取得します。
        /// </summary>
        private static Stack<INamedTypeSymbol> GetBaseTypes(INamedTypeSymbol type)
        {
            var result = new Stack<INamedTypeSymbol>();

            var current = type.BaseType;
            while (current != null)
            {
                result.Push(current);
                current = current.BaseType;
            }

            return result;
        }
    }
}
