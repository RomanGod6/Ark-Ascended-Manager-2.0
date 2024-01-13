﻿#pragma checksum "..\..\..\..\..\Views\Pages\ConfigPage.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "742124B5AE16D51361B1C858CD6621238F987D12"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Ark_Ascended_Manager.Helpers;
using Ark_Ascended_Manager.ViewModels.Pages;
using Ark_Ascended_Manager.Views.Pages;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using Wpf.Ui;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Wpf.Ui.Converters;
using Wpf.Ui.Markup;


namespace Ark_Ascended_Manager.Views.Pages {
    
    
    /// <summary>
    /// ConfigPage
    /// </summary>
    public partial class ConfigPage : System.Windows.Controls.Page, System.Windows.Markup.IComponentConnector {
        
        
        #line 101 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox SearchBoxGameIni;
        
        #line default
        #line hidden
        
        
        #line 108 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel Gameini;
        
        #line default
        #line hidden
        
        
        #line 118 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RichTextBox richTextPreview;
        
        #line default
        #line hidden
        
        
        #line 963 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox OverrideTextBox;
        
        #line default
        #line hidden
        
        
        #line 1131 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListBox lstPlugins;
        
        #line default
        #line hidden
        
        
        #line 1145 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal ICSharpCode.AvalonEdit.TextEditor jsonEditor;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/Ark Ascended Manager;V2.3.2;component/views/pages/configpage.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 76 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.AddNewScheduleButton_Click);
            
            #line default
            #line hidden
            return;
            case 2:
            
            #line 77 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.RestoreBackUpButton_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            
            #line 83 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.TabControl)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 4:
            this.SearchBoxGameIni = ((System.Windows.Controls.TextBox)(target));
            return;
            case 5:
            
            #line 102 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.SearchButtonGameini_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            
            #line 107 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.ScrollViewer)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 7:
            this.Gameini = ((System.Windows.Controls.StackPanel)(target));
            
            #line 108 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            this.Gameini.PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 8:
            
            #line 109 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.Expander)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 9:
            
            #line 110 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.StackPanel)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 10:
            this.richTextPreview = ((System.Windows.Controls.RichTextBox)(target));
            return;
            case 11:
            
            #line 177 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.StackPanel)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 12:
            
            #line 302 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.StackPanel)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 13:
            
            #line 325 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.StackPanel)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 14:
            
            #line 417 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.StackPanel)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 15:
            
            #line 682 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.StackPanel)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 16:
            
            #line 758 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.StackPanel)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 17:
            
            #line 811 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.StackPanel)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 18:
            
            #line 816 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.StackPanel)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 19:
            
            #line 821 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.StackPanel)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 20:
            
            #line 895 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.ListView)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 21:
            
            #line 931 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.TextBox)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 22:
            
            #line 953 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.ScrollViewer)(target)).PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 23:
            
            #line 962 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.CheckBox)(target)).Checked += new System.Windows.RoutedEventHandler(this.OverrideCheckBox_Checked);
            
            #line default
            #line hidden
            
            #line 962 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.CheckBox)(target)).Unchecked += new System.Windows.RoutedEventHandler(this.OverrideCheckBox_Unchecked);
            
            #line default
            #line hidden
            return;
            case 24:
            this.OverrideTextBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 25:
            this.lstPlugins = ((System.Windows.Controls.ListBox)(target));
            
            #line 1133 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            this.lstPlugins.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.LstPlugins_SelectionChanged);
            
            #line default
            #line hidden
            
            #line 1133 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            this.lstPlugins.PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ScrollViewer_PreviewMouseWheel);
            
            #line default
            #line hidden
            return;
            case 26:
            
            #line 1139 "..\..\..\..\..\Views\Pages\ConfigPage.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.SaveJsonButton_Click);
            
            #line default
            #line hidden
            return;
            case 27:
            this.jsonEditor = ((ICSharpCode.AvalonEdit.TextEditor)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

