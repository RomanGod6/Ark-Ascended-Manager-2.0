﻿#pragma checksum "..\..\..\..\..\Views\Pages\Integrations.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "2BF4A6226616560C0D22B354E7CA0D933E14415C"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

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


namespace Ark_Ascended_Manager.Views.Pages {
    
    
    /// <summary>
    /// IntegrationsPage
    /// </summary>
    public partial class IntegrationsPage : System.Windows.Controls.Page, System.Windows.Markup.IComponentConnector {
        
        
        #line 12 "..\..\..\..\..\Views\Pages\Integrations.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox BotTokenTextBox;
        
        #line default
        #line hidden
        
        
        #line 16 "..\..\..\..\..\Views\Pages\Integrations.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox GuildIdTextBox;
        
        #line default
        #line hidden
        
        
        #line 18 "..\..\..\..\..\Views\Pages\Integrations.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox WebhookUrlTextBox;
        
        #line default
        #line hidden
        
        
        #line 21 "..\..\..\..\..\Views\Pages\Integrations.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button SaveTokenButton;
        
        #line default
        #line hidden
        
        
        #line 22 "..\..\..\..\..\Views\Pages\Integrations.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button StartBotButton;
        
        #line default
        #line hidden
        
        
        #line 23 "..\..\..\..\..\Views\Pages\Integrations.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button StopBotButton;
        
        #line default
        #line hidden
        
        
        #line 24 "..\..\..\..\..\Views\Pages\Integrations.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock BotStatusTextBlock;
        
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
            System.Uri resourceLocater = new System.Uri("/Ark Ascended Manager;V2.3.2;component/views/pages/integrations.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\..\Views\Pages\Integrations.xaml"
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
            this.BotTokenTextBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 2:
            this.GuildIdTextBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 3:
            this.WebhookUrlTextBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 4:
            this.SaveTokenButton = ((System.Windows.Controls.Button)(target));
            
            #line 21 "..\..\..\..\..\Views\Pages\Integrations.xaml"
            this.SaveTokenButton.Click += new System.Windows.RoutedEventHandler(this.SaveTokenButton_Click);
            
            #line default
            #line hidden
            return;
            case 5:
            this.StartBotButton = ((System.Windows.Controls.Button)(target));
            
            #line 22 "..\..\..\..\..\Views\Pages\Integrations.xaml"
            this.StartBotButton.Click += new System.Windows.RoutedEventHandler(this.StartBotButton_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            this.StopBotButton = ((System.Windows.Controls.Button)(target));
            
            #line 23 "..\..\..\..\..\Views\Pages\Integrations.xaml"
            this.StopBotButton.Click += new System.Windows.RoutedEventHandler(this.StopBotButton_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            this.BotStatusTextBlock = ((System.Windows.Controls.TextBlock)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

