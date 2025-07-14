using System;
using System.Collections.Generic;
using System.IO;
using BehaviorTree.BehaviorTrees;
using BehaviorTree.Core;
using Editor.View.BtWindows.Core;
using Editor.View.BtWindows.MenuBar.Core;
using ExTools;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Save.FileStorage;
using UnityEditor;
using UnityEngine;

namespace Editor.View.BtWindows.MenuBar.Storage
{
    public class FileMenuItemBase : MenuBarItemBase
    {
        public FileMenuItemBase() : base("文件")
        {
        }

        protected override List<MenuItemData> CreateMenuItems()
        {
            var ownerWindow = BehaviorTreeWindows.FocusedWindow;
            if (ownerWindow == null)
            {
                Debug.LogWarning("无法打开文件菜单，因为没有聚焦的行为树窗口。");
                return null;
            }

            var recent_file_sub_items=new List<MenuItemData>();
            var file_record = FileRecordManager.Instance.FilePathStorage;

            foreach (var record in file_record.FileRecords)
            {
                var current_record = record;

                Action action = () => {CloseMenuAfterAction(() =>
                {
                    string file_path = current_record.FullPath;

                    if (string.IsNullOrEmpty(file_path))
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,new LogEntry(LogLevel.kWarning,$"Recent file not found: {file_path}"));
                        return;
                    }
                    
                    var asset_data=AssetDatabase.LoadAssetAtPath<BtWindowAsset>(file_path);
                    if (asset_data==null)
                    {
                        return;
                    }

                    var tree = BehaviorTreeManagers.instance.FindTreeByFilePath(
                        asset_data.GetAbsoluteExternalDatePath());

                    if (tree==null)
                    {
                        var temp_tree = new BehaviorTreeTemp(asset_data);
                        BehaviorTreeManagers.instance.RegisterTree(temp_tree.GetTreeId(), temp_tree);
                        BehaviorTreeWindowsBase.CreateWindowForTrees<BehaviorTreeWindows>(temp_tree.GetTreeId());
                    }
                    else
                    {
                        BehaviorTreeWindowsBase.CreateWindowForTrees<BehaviorTreeWindows>(tree.GetTreeId());
                    }
                }); };
                
                recent_file_sub_items.Add(new MenuItemData(current_record.DisplayName,true,action));
            }
            
            var menu_items = new List<MenuItemData>
            {
                new("新建行为树", true, () =>
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                        new LogEntry(LogLevel.kInfo, "FileMenuItem: 选择了'新建行为树'"));
                    CloseMenuAfterAction(() =>
                    {
                        var temp_tree = BehaviorTreeWindowsBase.CreateTempTree();
                        BehaviorTreeManagers.instance.RegisterTree(temp_tree.GetTreeId(), temp_tree);
                        BehaviorTreeWindowsBase.CreateWindowForTrees<BehaviorTreeWindows>(temp_tree.GetTreeId());
                    });
                }),
                new("打开文件", true, () =>
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                        new LogEntry(LogLevel.kInfo, "FileMenuItem: 选择了'打开文件'"));
                    CloseMenuAfterAction(() =>
                    {
                        var file_path = EditorUtility.OpenFilePanel("打开文件", Application.dataPath, "asset");

                        if (string.IsNullOrEmpty(file_path)) return;

                        var asset_data =
                            AssetDatabase.LoadAssetAtPath<BtWindowAsset>(
                                PathUtility.Instance.AbsoluteToRelativePath(file_path));

                        if (asset_data == null) return;

                        var tree = BehaviorTreeManagers.instance.FindTreeByFilePath(
                            asset_data.GetAbsoluteExternalDatePath());

                        if (tree == null)
                        {
                            var temp_tree = new BehaviorTreeTemp(asset_data);
                            BehaviorTreeManagers.instance.RegisterTree(temp_tree.GetTreeId(), temp_tree);
                            BehaviorTreeWindowsBase.CreateWindowForTrees<BehaviorTreeWindows>(temp_tree.GetTreeId());
                        }
                        else
                        {
                            BehaviorTreeWindowsBase.CreateWindowForTrees<BehaviorTreeWindows>(tree.GetTreeId());
                        }
                    });
                }),
                new MenuItemData("打开最近文件",recent_file_sub_items){Enabled = recent_file_sub_items.Count > 0},
                MenuItemData.CreateSeparator(),
                new("保存文件", ownerWindow != null, () =>
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                        new LogEntry(LogLevel.kInfo, "FileMenuItem: 选择了'保存文件'"));
                    CloseMenuAfterAction(() =>
                    {
                        if (ownerWindow) ownerWindow.SaveWindow();
                    });
                }),
                new("将文件保存到", true, () =>
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space,
                        new LogEntry(LogLevel.kInfo, "FileMenuItem: 选择了'将文件保存到'"));
                    CloseMenuAfterAction(() =>
                    {
                        ownerWindow.SaveWindow();

                        var tree = BehaviorTreeManagers.instance.GetTreeByWindowId(ownerWindow.WindowInstanceId);
                        if (tree == null)
                            return;

                        var path = EditorUtility.SaveFilePanelInProject("将文件保存到", tree.GetWindowAsset().name, "asset",
                            "Please select a location to save the BtWindowAsset.");

                        if (string.IsNullOrEmpty(path))
                        {
                            ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                                log_space,
                                new LogEntry(LogLevel.kInfo, "BtWindowAsset creation cancelled by user."));
                            return;
                        }

                        PathUtility.Instance.MoveAsset(
                            AssetDatabase.GetAssetPath(tree.GetWindowAsset().GetInstanceID()),
                            path);
                    });
                }),
                
            };

            return menu_items;
        }
    }
}