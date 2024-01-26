﻿using Microsoft.Win32;
using PSO.PRS;
using RDXplorer.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace RDXplorer.Views
{
    public static class Program
    {
        public static bool IsClosing { get; set; }

        public static DirectoryInfo TempPath { get; set; }

        public static void Initialize(MainWindow main)
        {
            Windows.Main = main;

            Models.AppView = new();
            main.AppViewModel = Models.AppView;
            main.DataContext = Models.AppView;
        }

        public static FileInfo SelectFile() =>
            SelectFile((FileInfo)null);

        public static FileInfo SelectFile(string path) =>
            SelectFile(new FileInfo(path));

        public static FileInfo SelectFile(FileInfo path)
        {
            OpenFileDialog dialog = new()
            {
                Multiselect = false,
                CheckFileExists = true,
                FileName = path != null && path.Exists ? path.FullName : string.Empty
            };

            if (dialog.ShowDialog() == null || !File.Exists(dialog.FileName))
                return null;

            return new FileInfo(dialog.FileName);
        }

        public static void SetTempPath()
        {
            try
            {
                TempPath = new DirectoryInfo($"{Properties.Settings.Default.tmp_path}\\{Guid.NewGuid()}");

                if (!TempPath.Exists)
                    TempPath.Create();

                TempPath.Refresh();
            }
            catch { }
        }

        public static void DeleteTempPath()
        {
            try
            {
                TempPath.Refresh();

                if (TempPath.Exists)
                    TempPath.Delete(true);

                TempPath.Refresh();
            }
            catch { }
        }

        public static void OpenURL(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    Process.Start("xdg-open", url);
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    Process.Start("open", url);
                else
                    throw;
            }
        }

        public static void LoadRDX(FileInfo file)
        {
            try
            {
                if (file == null)
                    return;

                using (Stream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    BinaryReader br = new(stream);

                    int magic = br.ReadInt32();

                    if (magic == 0x200000DF)
                    {
                        stream.Seek(0, SeekOrigin.Begin);

                        FileInfo tmp_file = new($"{TempPath.FullName}\\{Utilities.GetFileMD5(stream)}");

                        if (!tmp_file.Directory.Exists)
                            tmp_file.Directory.Create();

                        if (!tmp_file.Exists)
                            File.WriteAllBytes(tmp_file.FullName, PRS.Decompress(br.ReadBytes((int)file.Length)));

                        file = tmp_file;
                        file.Refresh();
                    }
                }

                bool isValid = false;

                using (Stream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    BinaryReader br = new(stream);
                    int magic = br.ReadInt32();
                    isValid = magic == 0x41200000 || magic == 0x40051EB8;
                }

                if (isValid)
                    Models.AppView.LoadRDX(file);

                if (Windows.HexEditor.IsVisible)
                {
                    Windows.HexEditor.ShowFile(file);
                    Windows.Main.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Application Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void OpenRDX()
        {
            try
            {
                FileInfo file = SelectFile();

                if (file == null)
                    return;

                Models.AppView.LoadFileList(file);

                // Fires Event MainWindow.FileList_SelectionChanged() > Program.LoadRDX()
                Windows.Main.FileList.SelectedValue = file.FullName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Application Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void CloseRDX()
        {
            try
            {
                Windows.HexEditor.HexEdit.CloseProvider();
                Models.AppView.UnloadRDX();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Application Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void CloseApp()
        {
            IsClosing = true;

            Program.DeleteTempPath();
            Program.CloseRDX();
            Windows.CloseAll();
        }

        public static class Windows
        {
            private static MainWindow _main;
            public static MainWindow Main
            {
                get
                {
                    if (_main == null)
                        _main = new();

                    return _main;
                }

                set
                {
                    _main = value;
                }
            }

            private static HexEditorWindow _hexEditor;
            public static HexEditorWindow HexEditor
            {
                get
                {
                    if (_hexEditor == null)
                        _hexEditor = new();

                    return _hexEditor;
                }

                set
                {
                    _hexEditor = value;
                }
            }

            private static AboutWindow _about;
            public static AboutWindow About
            {
                get
                {
                    if (_about == null)
                        _about = new();

                    return _about;
                }

                set
                {
                    _about = value;
                }
            }

            public static void CloseAll()
            {
                _hexEditor?.Close();
                _about?.Close();
            }
        }

        public static class Models
        {
            public static AppViewModel AppView { get; set; }
        }
    }
}