﻿#pragma checksum "..\..\..\..\..\Views\Pages\IssueReportForm.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "58DF1067563275E7880A21CA689A84CFEA6AEA8F"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Ark_Ascended_Manager.Views.Pages;
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
    /// IssueReportForm
    /// </summary>
    public partial class IssueReportForm : System.Windows.Controls.Page, System.Windows.Markup.IComponentConnector {
        
        
        #line 12 "..\..\..\..\..\Views\Pages\IssueReportForm.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox IssueTitle;
        
        #line default
        #line hidden
        
        
        #line 15 "..\..\..\..\..\Views\Pages\IssueReportForm.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox DiscordUsername;
        
        #line default
        #line hidden
        
        
        #line 18 "..\..\..\..\..\Views\Pages\IssueReportForm.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox IssueDescription;
        
        #line default
        #line hidden
        
        
        #line 21 "..\..\..\..\..\Views\Pages\IssueReportForm.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox AttachmentLink;
        
        #line default
        #line hidden
        
        
        #line 24 "..\..\..\..\..\Views\Pages\IssueReportForm.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox AdditionalInformation;
        
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
            System.Uri resourceLocater = new System.Uri("/Ark Ascended Manager;V2.2.2.0;component/views/pages/issuereportform.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\..\Views\Pages\IssueReportForm.xaml"
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
            this.IssueTitle = ((System.Windows.Controls.TextBox)(target));
            return;
            case 2:
            this.DiscordUsername = ((System.Windows.Controls.TextBox)(target));
            return;
            case 3:
            this.IssueDescription = ((System.Windows.Controls.TextBox)(target));
            return;
            case 4:
            this.AttachmentLink = ((System.Windows.Controls.TextBox)(target));
            return;
            case 5:
            this.AdditionalInformation = ((System.Windows.Controls.TextBox)(target));
            return;
            case 6:
            
            #line 26 "..\..\..\..\..\Views\Pages\IssueReportForm.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.SubmitButton_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

