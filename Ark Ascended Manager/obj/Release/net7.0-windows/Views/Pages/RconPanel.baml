   M S B A M L   `   `   `  ����   Ark Ascended Manager4    .Ark_Ascended_Manager.Views.Pages.RconPanelPage   W SPresentationCore, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      R NSystem.Xaml, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006       N� Title$  
Rcon Panel��.���  � ColumnDefinitions �� $
��Auto ��� $��* ��� $
��Auto �.+��� $��0�� �� Orientation$ Vertical=�$��10q�.+��� $��RCON Status��$
��Bold"��� ���� *
RconStatus+����  �� 
TargetType" �� �� Triggers ��  �� Binding �� *
RconStatus+ �� Value Disconnected.a���  �� Property � �� �� Value
 �  ����  �� *
RconStatus+ 	Connected.a���  � ��
 � � ���  �� *
RconStatus+
 Online.a���  � ��
 � � ���  �� *
RconStatus+ No Players Connected.a���  � ��
 � ���� $��1��	 � RowDefinitions	 �� $
��Auto ��� $
��1100 ��� $��99 ��� $��12.62 �.+��� $��0��
 �� DisplayMemberPath$
 
ServerName�� �� SelectedValuePath$ 
ServerName��$��40��$	��Top=����� *ServerProfiles+ �� SelectedItem �� *SelectedServerProfile+�� $��1��$
��Auto=�$��Disabled=�.���� $��0,0,0,10q�$	��100��$	��100������ *
ServerChat+���� .R��� ���� ]� $��2��$	��Top=�$��-91,0,-144,0q�$��2��.+��� $	��Top=�$��0,0,0,10q�$��35������ *CommandInput+�� $ 
Horizontal=�$	��Top=�$��1q�.+��� $
��Send��$��35��$��75��$��1q����� *SendCommand+�� $��
Clear Chat��$��35��$��75��$��1q�$��5q����� *ClearChatCommand+�� $��2��$��10q�.+��� $��Copy ID������ *CopyIdCommand+���� *SelectedPlayerInfo+{��� *SelectedPlayerInfo+�� $��Connected Players:��$
��Bold"�$��0,0,0,10q��� -    ��Name$ PlayersListBox������ *ConnectedPlayers+ �� *SelectedPlayerInfo+���� " ��.���  �@�ContextMenu ��  � DataContext ��  �� Path$ DataContext� �� RelativeSource ��  �� AncestorType" ��.@�m� $��Copy ID�� m� Command �� $ DataContext.CopyIdCommand� ��  ContextMenu m� CommandParameter �� 