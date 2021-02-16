using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SysDraw = System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace WPFTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Canvas canvas, canvasParent, canvasHandler, canvasDragger, canvasMatrix;
        StatusBarItem status;
        //Grid colourChooser;
        Color colour = Colors.Black, selectionColour = Colors.CadetBlue, deletionColour = Colors.Red;
        Tool currentTool;
        CanvasAction currentAction = CanvasAction.IDLE;
        Document openDocument;
        Point lastCanvasPoint, auxCanvasPoint, lastDragPoint, lassoStart;
        List<Edge> selection = new List<Edge>();
        int pointSize = 20, lineThickness = 2, pointThickness = 3, xOffset = 0, yOffset = 0;
        bool stepLock = true, pointLock = true, windowLoaded = false, isPanning = false, showPoints = true, showSteps = false, drawLocator = true, lassoing = false;
        string openDocumentSaveLocation = "";
        MouseButtonState leftButton, middleButton, rightButton;
        bool ctrlMod = false, altMod = false, shiftMod = false;

        public bool forceKill = false;

        public MainWindow()
        {
            this.InitializeComponent();
            double pmh = this.MinHeight;
            double pmw = this.MinWidth;
            double ph = this.Height;
            double pw = this.Width;
            this.MinHeight = 0;
            this.MinWidth = 0;
            this.Height = 0;
            this.Width = 0;
            this.WindowStyle = WindowStyle.None;
            this.Hide();
            Splash splash = new Splash();
            (new Thread(() => {
                Thread.Sleep(5000);
                this.Dispatcher.Invoke(() => {
                    splash.Close();
                    this.Show();
                    this.Height = ph;
                    this.Width = pw;
                    this.MinHeight = pmh;
                    this.MinWidth = pmw;
                    this.Top = (SystemParameters.PrimaryScreenHeight / 2) - (ph / 2);
                    this.Left = (SystemParameters.PrimaryScreenWidth / 2) - (pw / 2);
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.Activate();
                    this.DrawDocument(true);
                });
            })).Start();

            openDocument = new Document(DrawingType.ORTHOGRAPHIC, 20, 20); //TEMPORARY
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = Properties.Resources.AppNameVersion;
            canvas = (Canvas)this.FindName("Canvas");
            canvasParent = (Canvas)this.FindName("CanvasParent");
            canvasHandler = (Canvas)this.FindName("CanvasEH");
            canvasDragger = (Canvas)this.FindName("CanvasDH");
            canvasMatrix = (Canvas)this.FindName("CanvasMX");
            status = (StatusBarItem)this.FindName("StatusBar");
            //colourChooser = (Grid)this.FindName("ColourChooser");
            windowLoaded = true;
            ChangeTool(Tool.LINE);
            DrawDocument(true);
        }

        private void DrawDocument(bool drawMatrix = false)
        {
            if (!windowLoaded) return;
            canvas.Width = openDocument.size.Width * pointSize * (openDocument.type == DrawingType.ISOMETRIC ? Helper.Isometric.eqHeight : 1);
            canvas.Height = openDocument.size.Height * pointSize;
            canvasHandler.Width = canvas.Width;
            canvasHandler.Height = canvas.Height;
            canvasDragger.Width = canvas.Width;
            canvasDragger.Height = canvas.Height;
            canvasMatrix.Width = canvas.Width;
            canvasMatrix.Height = canvas.Height;
            canvas.Margin = new Thickness(((canvasParent.ActualWidth - canvas.Width) / 2) + (double)xOffset, ((canvasParent.ActualHeight - canvas.Height) / 2) + (double)yOffset, 0, 0);
            canvasHandler.Margin = canvas.Margin;
            canvasDragger.Margin = canvas.Margin;
            canvasMatrix.Margin = canvas.Margin;
            canvas.Children.Clear();
            if (drawMatrix) {
                canvasMatrix.Children.Clear();
                Thread cthread = new Thread(() =>
                {
                    List<Thickness> matrix = new List<Thickness>();
                    List<Thickness> subMatrix = new List<Thickness>();
                    if (openDocument.type == DrawingType.ORTHOGRAPHIC)
                    {
                        if (showSteps)
                            for (int i = 0; i < openDocument.size.Width * openDocument.stepDivision; i++)
                                for (int j = 0; j < openDocument.size.Height * openDocument.stepDivision; j++)
                                {
                                    subMatrix.Add(new Thickness((double)i * ((double)pointSize / (double)openDocument.stepDivision) - (double)((pointThickness - 1) / 2), (double)j * ((double)pointSize / (double)openDocument.stepDivision) - (double)((pointThickness - 1) / 2), 0, 0));
                                }
                        if (showPoints)
                            for (int i = 0; i < openDocument.size.Width; i++)
                                for (int j = 0; j < openDocument.size.Height; j++)
                                {
                                    matrix.Add(new Thickness(i * pointSize - ((pointThickness - 1) / 2), j * pointSize - ((pointThickness - 1) / 2), 0, 0));
                                }
                    }
                    else if (openDocument.type == DrawingType.ISOMETRIC)
                    {
                        if (showSteps)
                            for (int i = 0; i < openDocument.size.Width * openDocument.stepDivision; i++)
                                for (int j = 0; j < openDocument.size.Height * openDocument.stepDivision; j++)
                                {
                                    subMatrix.Add(new Thickness(i * Helper.Isometric.eqHeight * (double)pointSize * (1d / (double)openDocument.stepDivision) - (double)((pointThickness - 1) / 2), (double)j * (double)pointSize * (1d / (double)openDocument.stepDivision) + (i % 2 == 1 ? (0.5d / (double)openDocument.stepDivision) * (double)pointSize : 0d) - (double)((pointThickness - 1) / 2), 0, 0));
                                }
                        if (showPoints)
                            for (int i = 0; i < openDocument.size.Width; i++)
                                for (int j = 0; j < openDocument.size.Height; j++)
                                {
                                    matrix.Add(new Thickness(i * Helper.Isometric.eqHeight * pointSize - ((pointThickness - 1) / 2), j * pointSize + (i % 2 == 1 ? 0.5 * pointSize : 0) - ((pointThickness - 1) / 2), 0, 0));
                                }
                    }
                    this.Dispatcher.Invoke(() =>
                    {
                        SolidColorBrush lightGray = Brushes.LightGray;
                        foreach (Thickness dot in subMatrix) canvasMatrix.Children.Add(new Ellipse() {
                            Width = pointThickness,
                            Height = pointThickness,
                            Fill = lightGray,
                            Margin = dot,
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Left
                        });
                        SolidColorBrush darkGray = Brushes.DarkGray;
                        foreach (Thickness dot in matrix) canvasMatrix.Children.Add(new Ellipse()
                        {
                            Width = pointThickness,
                            Height = pointThickness,
                            Fill = darkGray,
                            Margin = dot,
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Left
                        });
                        matrix.Clear();
                        subMatrix.Clear();
                    });
                });
                cthread.Name = "Async eval process";
                cthread.Start();
            }
            List<Line> lines = new List<Line>();
            foreach (Edge edge in openDocument.edges)
            {
                bool selected = selection.Contains(edge);
                Point l1 = new Point(edge.x1 * pointSize, edge.y1 * pointSize);
                Point l2 = new Point(edge.x2 * pointSize, edge.y2 * pointSize);
                if (openDocument.type == DrawingType.ISOMETRIC)
                {
                    l1 = Helper.Isometric.IsoToOrtho(l1);
                    l2 = Helper.Isometric.IsoToOrtho(l2);
                }
                Line tmp = new Line()
                {
                    X1 = l1.X,
                    X2 = l2.X,
                    Y1 = l1.Y,
                    Y2 = l2.Y,
                    StrokeThickness = selected ? 5 : lineThickness,
                    Stroke = selected ? new SolidColorBrush(selectionColour) : new SolidColorBrush(Color.FromRgb(edge.r, edge.g, edge.b)),
                    Fill = new SolidColorBrush(Color.FromRgb(edge.r, edge.g, edge.b))
                };
                tmp.MouseEnter += (object sender, MouseEventArgs e) => { tmp.Stroke = new SolidColorBrush(deletionColour); tmp.StrokeThickness = 5; };
                tmp.MouseLeave += (object sender, MouseEventArgs e) => { tmp.Stroke = tmp.Fill; tmp.StrokeThickness = lineThickness; };
                tmp.MouseDown += (object sender, MouseButtonEventArgs e) => { openDocument.SetEdge(new Point(tmp.X1 / pointSize, tmp.Y1 / pointSize), new Point(tmp.X2 / pointSize, tmp.Y2 / pointSize), Colors.Black, false); DrawDocument(); };
                lines.Add(tmp);
            }
            foreach (Line line in lines) canvas.Children.Add(line);
            lines.Clear();
        }

        private void KeyPress(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    ctrlMod = true;
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    altMod = true;
                    break;
                case Key.LeftShift:
                case Key.RightShift:
                    shiftMod = true;
                    break;
                default:
                    altMod = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
                    ctrlMod = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                    shiftMod = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    if (ctrlMod)
                    {
                        switch (e.Key)
                        {
                            case Key.N:
                                if (!shiftMod) NewDoc();
                                break;
                            case Key.O:
                                if (!shiftMod) Open();
                                break;
                            case Key.S:
                                if (shiftMod) Save();
                                else SaveCnm();
                                break;
                            case Key.E:
                            case Key.P:
                                if (!shiftMod) Export();
                                break;
                            case Key.R:
                                if (!shiftMod) DrawDocument(true);
                                else SelectionRecolour(sender, e);
                                break;
                            case Key.OemPlus:
                            case Key.Add:
                                if (!shiftMod) ZoomIn(sender, e);
                                break;
                            case Key.OemMinus:
                            case Key.Subtract:
                                if (!shiftMod) ZoomOut(sender, e);
                                break;
                            case Key.D0:
                                if (!shiftMod) ZoomReset(sender, e);
                                break;
                            case Key.Q:
                                if (!shiftMod) Quit();
                                else Crash();
                                break;
                            case Key.F:
                                if (!shiftMod) break; //Ctrl+F means Find
                                else ShowFileSettings(sender, e);
                                break;
                            case Key.V:
                                if (!shiftMod) break; //Ctrl+V means Paste
                                else ShowViewSettings(sender, e);
                                break;
                            case Key.I:
                                if (!shiftMod) SelectOpp(sender, e);
                                break;
                            case Key.A:
                                if (!shiftMod) SelectAll(sender, e);
                                else SelectNone(sender, e);
                                break;
                            case Key.T:
                                if (!shiftMod) SelectionTransform(sender, e);
                                break;
                            case Key.D:
                                if (!shiftMod) SelectionDuplicate(sender, e);
                                break;
                            case Key.OemQuestion:
                                if (!shiftMod) ShowHelpDoc(sender, e);
                                break;
                            case Key.Y:
                            case Key.Z:
                                break; //I don't know how to implement undo/redo.
                        }
                    }
                    else
                    {
                        switch (e.Key)
                        {
                            case Key.Escape:
                                selection.Clear();
                                ChangeTool(currentTool);
                                DrawDocument();
                                break;
                            case Key.Delete:
                                if (!shiftMod) SelectionDelete(sender, e);
                                break;
                            case Key.BrowserRefresh:
                            case Key.F5:
                                DrawDocument(true);
                                break;
                            case Key.N:
                                if (!shiftMod) ToolNone_Click(sender, e);
                                break;
                            case Key.L:
                                if (!shiftMod) ToolLine_Click(sender, e);
                                else GridLock_Click(sender, e);
                                break;
                            case Key.R:
                                if (!shiftMod) ToolRect_Click(sender, e);
                                break;
                            case Key.P:
                                if (!shiftMod) ToolPoly_Click(sender, e);
                                else
                                {
                                    if ((bool)PanToggler.IsChecked) PanEnabler_Unchk(sender, e);
                                    else PanEnabler_Check(sender, e);
                                }
                                break;
                            case Key.E:
                                if (!shiftMod) ToolEras_Click(sender, e);
                                break;
                            case Key.S:
                                if (shiftMod)
                                {
                                    if (showSteps) DisableGridSteps(sender, e);
                                    else EnableGridSteps(sender, e);
                                }
                                break;
                            case Key.G:
                                if (shiftMod)
                                {
                                    if (showPoints) DisableGridPoints(sender, e);
                                    else EnableGridPoints(sender, e);
                                }
                                break;
                            case Key.V:
                                if (shiftMod) ShowViewSettings(sender, e);
                                break;
                            case Key.C:
                                if (shiftMod) Colour_Click(sender, e);
                                break;
                            case Key.F:
                                if (shiftMod) ShowFileSettings(sender, e);
                                break;
                        }
                    }
                    break;
            }
        }
        private void KeyPressEnd(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    ctrlMod = false;
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    altMod = false;
                    break;
                case Key.LeftShift:
                case Key.RightShift:
                    shiftMod = false;
                    break;
            }
        }

        private void ZoomIn(object sender, RoutedEventArgs e)
        {
            //stepSize = Math.Max(stepSize + 1, 1); //POTENTIAL BUG
            pointSize = Math.Max(pointSize + 2, 1);
            DrawDocument(true);
        }
        private void ZoomOut(object sender, RoutedEventArgs e)
        {
            //stepSize = Math.Max(stepSize - 1, 1);
            pointSize = Math.Max(pointSize - 2, 1);
            DrawDocument(true);
        }
        private void ZoomReset(object sender, RoutedEventArgs e)
        {
            xOffset *= (20 / pointSize);
            yOffset *= (20 / pointSize);
            //stepSize = 10;
            pointSize = 20;
            DrawDocument(true);
        }

        private void SaveCnm(object sender, RoutedEventArgs e) { SaveCnm(); }
        private void SaveCnm() {
            if (openDocumentSaveLocation != "") Save(openDocumentSaveLocation);
            else Save();
        }
        private void Save(object sender, RoutedEventArgs e) { Save(); }
        private void Save()
        {
            if (openDocument.edges.Count < 1) {
                MessageBox.Show("Cannot save an empty drawing!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SaveFileDialog saveDialog = new SaveFileDialog()
            {
                AddExtension = true,
                Filter = "IsoDraw Files|*.idt",
                Title = "Save Drawing"
            };
            saveDialog.ShowDialog();
            if (saveDialog.FileName != "") Save(saveDialog.FileName);
        }
        private async void Save(string path)
        {
            if (path == "") return;
            openDocumentSaveLocation = path;
            Helper.ProgressAlert saveProgress = new Helper.ProgressAlert()
            {
                Title = "Saving file..."
            };
            saveProgress.ShowProgress();
            File.WriteAllText(path, openDocument.ToString());
            Progress<bool> progress = new Progress<bool>(s => saveProgress.Close());
            await Task.Factory.StartNew(() => {
                Thread.Sleep(500);
                ((IProgress<bool>)progress).Report(true);
            }, TaskCreationOptions.LongRunning);
        }

        private void Open(object sender, RoutedEventArgs e) { Open(); }
        private void Open()
        {
            if (openDocument.edges.Count > 0) if (MessageBox.Show("Unsaved changes will be lost. Proceed?", "Open Drawing", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
            OpenFileDialog openDialog = new OpenFileDialog()
            {
                Filter = "IsoDraw Files|*.idt",
                Title = "Open Drawing"
            };
            openDialog.ShowDialog();
            if (openDialog.FileName != "") Open(openDialog.FileName);
        }
        private async void Open(string path)
        {
            if (path == "") return;
            if (!File.Exists(path))
            {
                MessageBox.Show("Cannot open a nonexistent file! Check the filepath.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            openDocumentSaveLocation = path;
            Helper.ProgressAlert openProgres = new Helper.ProgressAlert()
            {
                Title = "Opening file..."
            };
            openProgres.ShowProgress();
            Progress<bool> progress = new Progress<bool>(s => openProgres.Close());
            try
            {
                openDocument = new Document(File.ReadAllText(path));
                DrawDocument(true);
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid document format! Your file may be outdated or corrupted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            await Task.Factory.StartNew(() => {
                Thread.Sleep(500);
                ((IProgress<bool>)progress).Report(true);
            }, TaskCreationOptions.LongRunning);
        }

        private void NewDoc(object sender, RoutedEventArgs e) { NewDoc(); }
        private void NewDoc()
        {
            if (openDocument.edges.Count > 0) if (MessageBox.Show("Unsaved changes will be lost. Proceed?", "Create Drawing", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
            openDocument = new Document(DrawingType.ORTHOGRAPHIC, 20, 20);
            openDocumentSaveLocation = "";
            DrawDocument(true);
        }

        private void Export(object sender, RoutedEventArgs e) { Export(); }
        private void Export() //TEMPORARY
        {
            SaveFileDialog exportDialog = new SaveFileDialog()
            {
                Title = "Export Drawing",
                Filter = "Bitmap|*.bmp|JPEG Image|*.jpg;*.jpeg|PNG Image|*.png",
                AddExtension = true
            };
            exportDialog.ShowDialog();
            if (exportDialog.FileName != "") Export(exportDialog.FileName, 32);
        }
        private async void Export(string name, int pixels)
        {
            Helper.ProgressAlert progressAlert = new Helper.ProgressAlert()
            {
                Title = "Opening file..."
            };
            progressAlert.ShowProgress();
            Progress<bool> progress = new Progress<bool>(s => progressAlert.Close());
            
            SysDraw.Bitmap export = new SysDraw.Bitmap(pixels * (int)openDocument.size.Width, pixels * (int)openDocument.size.Height);
            SysDraw.Graphics exportGraphics = SysDraw.Graphics.FromImage(export);
            foreach (Edge ioEdge in openDocument.edges)
            {
                Edge edge = ioEdge;
                if (openDocument.type == DrawingType.ISOMETRIC)
                {
                    Point tmp = Helper.Isometric.IsoToOrtho(new Point(edge.x1, edge.y1));
                    edge.x1 = (float)tmp.X;
                    edge.y1 = (float)tmp.Y;
                    tmp = Helper.Isometric.IsoToOrtho(new Point(edge.x2, edge.y2));
                    edge.x2 = (float)tmp.X;
                    edge.y2 = (float)tmp.Y;
                }
                
                using (SysDraw.Pen pen = new SysDraw.Pen(SysDraw.Color.FromArgb(edge.r, edge.g, edge.b)))
                {
                    exportGraphics.DrawLine(pen, new SysDraw.Point((int)(edge.x1 * (float)pixels), (int)(edge.y1 * (float)pixels)), new SysDraw.Point((int)(edge.x2 * (float)pixels), (int)(edge.y2 * (float)pixels)));
                }
            }
            export.Save(name);

            await Task.Factory.StartNew(() => {
                Thread.Sleep(500);
                ((IProgress<bool>)progress).Report(true);
            }, TaskCreationOptions.LongRunning);

            Helper.RunCommand("explorer \"" + name + "\"");
        }

        private void SelectAll(object sender, RoutedEventArgs e)
        {
            selection.Clear();
            foreach (Edge edge in openDocument.edges) selection.Add(edge);
            DrawDocument();
        }
        private void SelectNone(object sender, RoutedEventArgs e)
        {
            selection.Clear();
            DrawDocument();
        }
        private void SelectOpp(object sender, RoutedEventArgs e)
        {
            List<Edge> tmp = new List<Edge>();
            foreach (Edge edge in openDocument.edges) if (!selection.Contains(edge)) tmp.Add(edge);
            selection = tmp;
            DrawDocument();
        }

        private void ShowViewSettings(object sender, RoutedEventArgs e)
        {
            ViewSetup vs = new ViewSetup(selectionColour, deletionColour, lineThickness, pointThickness);
            vs.DialogSubmit += (object s, ViewSetup.ViewSettingsEventArgs d) =>
            {
                selectionColour = d.selectionColour;
                deletionColour = d.deletionColour;
                lineThickness = d.lineThickness;
                pointThickness = d.pointThickness;
                DrawDocument(true);
            };
            vs.ShowDialog();
        }
        private void ShowFileSettings(object sender, RoutedEventArgs e)
        {
            FileSetup fs = new FileSetup(openDocument.type, openDocument.size, openDocument.stepDivision);
            fs.DialogSubmit += (object s, FileSetup.FileSettingsEventArgs d) =>
            {
                openDocument.size = d.size;
                openDocument.type = d.type;
                openDocument.stepDivision = d.stepDivision;
                DrawDocument(true);
            };
            fs.ShowDialog();
        }

        private void EnableGridPoints(object sender, RoutedEventArgs e) { ToggleGridPoints(true); }
        private void DisableGridPoints(object sender, RoutedEventArgs e) { ToggleGridPoints(false); }
        private void ToggleGridPoints(bool state)
        {
            if (!windowLoaded) return;
            showPoints = state;
            if (((MenuItem)FindName("MenuViewToggleGrid")).IsChecked != showPoints) ((MenuItem)FindName("MenuViewToggleGrid")).IsChecked = showPoints;
            if (((ToggleButton)FindName("ToggleGrid")).IsChecked != showPoints) ((ToggleButton)FindName("ToggleGrid")).IsChecked = showPoints;
            DrawDocument(true);
        }
        private void EnableGridSteps(object sender, RoutedEventArgs e) { ToggleGridSteps(true); }
        private void DisableGridSteps(object sender, RoutedEventArgs e) { ToggleGridSteps(false); }
        private void ToggleGridSteps(bool state)
        {
            if (!windowLoaded) return;
            showSteps = state;
            if (((MenuItem)FindName("MenuViewToggleSteps")).IsChecked != showSteps) ((MenuItem)FindName("MenuViewToggleSteps")).IsChecked = showSteps;
            if (((ToggleButton)FindName("ToggleSteps")).IsChecked != showSteps) ((ToggleButton)FindName("ToggleSteps")).IsChecked = showSteps;
            DrawDocument(true);
        }

        private void PanEnabler_Check(object sender, RoutedEventArgs e)
        {
            ((ComboBox)this.FindName("ToolSelector")).SelectedIndex = 0;
            ((ComboBox)this.FindName("ToolSelector")).IsEnabled = false;
            ChangeTool(Tool.NONE);
            StatusBar.Content = "Click and drag to move canvas";
            canvasDragger.Visibility = Visibility.Visible;
            if (((ToggleButton)FindName("PanToggler")).IsChecked != true) ((ToggleButton)FindName("PanToggler")).IsChecked = true;
            if (((MenuItem)FindName("MenuViewPanToggle")).IsChecked != true) ((MenuItem)FindName("MenuViewPanToggle")).IsChecked = true;
        }
        private void PanEnabler_Unchk(object sender, RoutedEventArgs e)
        {
            ((ComboBox)this.FindName("ToolSelector")).SelectedIndex = 1;
            ((ComboBox)this.FindName("ToolSelector")).IsEnabled = true;
            StatusBar.Content = "Ready";
            canvasDragger.Visibility = Visibility.Hidden;
            if (((ToggleButton)FindName("PanToggler")).IsChecked != false) ((ToggleButton)FindName("PanToggler")).IsChecked = false;
            if (((MenuItem)FindName("MenuViewPanToggle")).IsChecked != false) ((MenuItem)FindName("MenuViewPanToggle")).IsChecked = false;
        }

        private void ToolSelector_Change(object sender, SelectionChangedEventArgs e)
        {
            if (!windowLoaded) return;
            switch (((ComboBoxItem)e.AddedItems[0]).Content)
            {
                case "Line":
                    ToolLine_Click(sender, e);
                    break;
                case "Rectangle":
                    ToolRect_Click(sender, e);
                    break;
                case "Polygon":
                    ToolPoly_Click(sender, e);
                    break;
                case "Eraser":
                    ToolEras_Click(sender, e);
                    break;
                case "None":
                    ToolNone_Click(sender, e);
                    break;
            }
        }
        private void ToolNone_Click(object sender, RoutedEventArgs e) { ChangeTool(Tool.NONE); }
        private void ToolLine_Click(object sender, RoutedEventArgs e) { ChangeTool(Tool.LINE); }
        private void ToolRect_Click(object sender, RoutedEventArgs e) { ChangeTool(Tool.RECT); }
        private void ToolPoly_Click(object sender, RoutedEventArgs e) { ChangeTool(Tool.POLY); }
        private void ToolEras_Click(object sender, RoutedEventArgs e) { ChangeTool(Tool.DEL); }
        private void ChangeTool(Tool tool)
        {
            if (!windowLoaded) return;
            string promptMessage = "Ready", menuRadioName = "MenuDrawTool";
            int toolIndex;
            canvasHandler.Visibility = Visibility.Visible;
            switch (tool)
            {
                case Tool.NONE:
                    currentAction = CanvasAction.IDLE;
                    //canvasHandler.Cursor = Cursors.Arrow;
                    menuRadioName += "None";
                    toolIndex = 0;
                    break;
                case Tool.LINE:
                    currentAction = CanvasAction.LINE_PT1;
                    //canvasHandler.Cursor = Cursors.Cross;
                    menuRadioName += "Line";
                    promptMessage = "Select line startpoint";
                    toolIndex = 1;
                    break;
                case Tool.RECT:
                    currentAction = CanvasAction.RECT_PT1;
                    //canvasHandler.Cursor = Cursors.Cross;
                    menuRadioName += "Rect";
                    promptMessage = "Select first rectangle corner";
                    toolIndex = 2;
                    break;
                case Tool.POLY:
                    currentAction = CanvasAction.POLY_CTR;
                    //canvasHandler.Cursor = Cursors.Cross;
                    menuRadioName += "Poly";
                    promptMessage = "Select polygon centre point";
                    toolIndex = 3;
                    break;
                case Tool.DEL:
                    currentAction = CanvasAction.ERASE;
                    canvas.Cursor = Cursors.Cross; //Change
                    canvasHandler.Visibility = Visibility.Hidden;
                    menuRadioName += "Eras";
                    promptMessage = "Select line to remove";
                    toolIndex = 4;
                    break;
                default:
                    return;
            }
            if (((RadioButton)FindName(menuRadioName)).IsChecked == false) ((RadioButton)FindName(menuRadioName)).IsChecked = true;
            if (((ComboBox)FindName("ToolSelector")).SelectedIndex != toolIndex) ((ComboBox)FindName("ToolSelector")).SelectedIndex = toolIndex;
            status.Content = promptMessage;
            currentTool = tool;
            DrawDocument();
        }

        private void OpenWebsite(object sender, RoutedEventArgs e)
        {
            Helper.RunCommand("explorer " + Properties.Resources.Webpage);
        }
        private void ShowHelpDoc(object sender, RoutedEventArgs e)
        {
            //Show HelpDoc.xaml
        }

        private void Canvas_MouseDn(object sender, MouseButtonEventArgs e)
        {
            leftButton = e.LeftButton;
            middleButton = e.MiddleButton;
            rightButton = e.RightButton;
            if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released && e.MiddleButton == MouseButtonState.Released)
            {
                selection.Clear();
                string promptMessage = (string)status.Content;
                Point pos = e.GetPosition((Canvas)sender);
                if (openDocument.type == DrawingType.ORTHOGRAPHIC)
                {
                    if (pointLock) pos = new Point(Helper.RoundTo(pos.X, pointSize), Helper.RoundTo(pos.Y, pointSize));
                    else if (stepLock) pos = new Point(Helper.RoundTo(pos.X, (pointSize / openDocument.stepDivision)), Helper.RoundTo(pos.Y, (pointSize / openDocument.stepDivision)));
                } else if (openDocument.type == DrawingType.ISOMETRIC)
                {
                    if (pointLock || stepLock) { 
                        Point tmp = Helper.Isometric.OrthoToIso(pos);
                        pos = Helper.Isometric.IsoToOrtho(new Point(Helper.RoundTo(tmp.X, pointLock ? pointSize : pointSize / openDocument.stepDivision), Helper.RoundTo(tmp.Y, pointLock ? pointSize : pointSize / openDocument.stepDivision)));
                    }
                }
                if (currentAction == CanvasAction.LINE_PT1)
                {
                    currentAction = CanvasAction.LINE_PT2;
                    promptMessage = "Select line endpoint";
                } else if (currentAction == CanvasAction.LINE_PT2)
                {
                    Point p1 = new Point(lastCanvasPoint.X / pointSize, lastCanvasPoint.Y / pointSize);
                    Point p2 = new Point(pos.X / pointSize, pos.Y / pointSize);
                    if (openDocument.type == DrawingType.ISOMETRIC)
                    {
                        p1 = Helper.Isometric.OrthoToIso(p1);
                        p2 = Helper.Isometric.OrthoToIso(p2);
                    }
                    openDocument.SetEdge(p1, p2, colour);
                    currentAction = CanvasAction.LINE_PT1;
                    promptMessage = "Select line startpoint";
                } else if (currentAction == CanvasAction.RECT_PT1)
                {
                    currentAction = CanvasAction.RECT_PT2;
                    promptMessage = "Select second rectangle corner";
                } else if (currentAction == CanvasAction.RECT_PT2)
                {
                    Point p1 = new Point(lastCanvasPoint.X / pointSize, lastCanvasPoint.Y / pointSize);
                    Point p2 = new Point(pos.X / pointSize, pos.Y / pointSize);
                    if (openDocument.type == DrawingType.ISOMETRIC)
                    {
                        p1 = Helper.Isometric.OrthoToIso(p1);
                        p2 = Helper.Isometric.OrthoToIso(p2);
                    }
                    openDocument.SetEdge(p1, new Point(p1.X, p2.Y), colour);
                    openDocument.SetEdge(p1, new Point(p2.X, p1.Y), colour);
                    openDocument.SetEdge(p2, new Point(p1.X, p2.Y), colour);
                    openDocument.SetEdge(p2, new Point(p2.X, p1.Y), colour);
                    currentAction = CanvasAction.RECT_PT1;
                    promptMessage = "Select first rectangle corner";
                } else if (currentAction == CanvasAction.POLY_CTR)
                {
                    auxCanvasPoint = pos;
                    currentAction = CanvasAction.POLY_PT1;
                    promptMessage = "Select first polygon point";
                } else if (currentAction == CanvasAction.POLY_PT1)
                {
                    currentAction = CanvasAction.POLY_PT2;
                    promptMessage = "Select second polygon point";
                } else if (currentAction == CanvasAction.POLY_PT2)
                {
                    //Trigonometry is confusing.
                    double radius = Helper.Distance(auxCanvasPoint, lastCanvasPoint);
                    int ngonSides = Math.Max(3, (int)Math.Round(360 / Math.Abs(Helper.AngleBetweenPoints(pos, lastCanvasPoint, auxCanvasPoint))));
                    double intAng = 360 / ngonSides;
                    double angOfs = Math.Abs(Helper.AngleBetweenPoints(lastCanvasPoint, new Point(auxCanvasPoint.X, auxCanvasPoint.Y + 1), auxCanvasPoint));
                    for (int i = 0; i < ngonSides; i++)
                    {
                        Point start = Helper.PointOnCircleCircumference(auxCanvasPoint, radius, (angOfs + (intAng * i)) % 360);
                        Point end = Helper.PointOnCircleCircumference(auxCanvasPoint, radius, (angOfs + (intAng * (i + 1))) % 360);
                        if (openDocument.type == DrawingType.ISOMETRIC)
                        {
                            start = Helper.Isometric.OrthoToIso(start);
                            end = Helper.Isometric.OrthoToIso(end);
                        }
                        openDocument.SetEdge(new Tuple<double, double>(start.X / pointSize, start.Y / pointSize), new Tuple<double, double>(end.X / pointSize, end.Y / pointSize), colour);
                    }
                    currentAction = CanvasAction.POLY_CTR;
                    promptMessage = "Select polygon centre point";
                }
                lastCanvasPoint = pos;
                status.Content = promptMessage;
                DrawDocument();
            } else if (e.RightButton == MouseButtonState.Pressed && e.LeftButton == MouseButtonState.Released && e.MiddleButton == MouseButtonState.Released)
            {
                lassoing = true;
                lassoStart = e.GetPosition((Canvas)sender);
                ChangeTool(Tool.NONE);
                ToolSelector.SelectedIndex = 0;
            } else if (e.MiddleButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released && e.LeftButton == MouseButtonState.Released)
            {
                PanEnabler_Check(sender, new RoutedEventArgs());
                isPanning = true;
                lastDragPoint = e.GetPosition(canvasParent);
            }
        }
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (rightButton == MouseButtonState.Pressed && leftButton == MouseButtonState.Released && middleButton == MouseButtonState.Released)
            {
                lassoing = false;
                Point lassoEndRaw = e.GetPosition((Canvas)sender);

                Point lassoEnd = new Point(Math.Max(lassoStart.X, lassoEndRaw.X) / pointSize, Math.Max(lassoStart.Y, lassoEndRaw.Y) / pointSize);
                lassoStart = new Point(Math.Min(lassoStart.X, lassoEndRaw.X) / pointSize, Math.Min(lassoStart.Y, lassoEndRaw.Y) / pointSize);

                if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift)) selection.Clear(); //Allow shift selection
                foreach (Edge flioEdge in openDocument.edges)
                {
                    Edge edge = flioEdge;
                    //if (openDocument.type == DrawingType.ISOMETRIC)
                    //{
                    //    Point tmp = Helper.Isometric.IsoToOrtho(new Point(edge.x1, edge.y1));
                    //    edge.x1 = (float)tmp.X;
                    //    edge.y1 = (float)tmp.Y;
                    //    tmp = Helper.Isometric.IsoToOrtho(new Point(edge.x2, edge.y2));
                    //    edge.x2 = (float)tmp.X;
                    //    edge.y2 = (float)tmp.Y;
                    //}
                    if (lassoStart.X < edge.x1 && lassoStart.Y < edge.y1 && lassoEnd.X > edge.x1 && lassoEnd.Y > edge.y1) if (lassoStart.X < edge.x2 && lassoStart.Y < edge.y2 && lassoEnd.X > edge.x2 && lassoEnd.Y > edge.y2) if (!selection.Contains(edge)) selection.Add(edge);
                }
                DrawDocument();
            } else if (rightButton == MouseButtonState.Released && leftButton == MouseButtonState.Released && middleButton == MouseButtonState.Pressed)
            {
                PanEnabler_Unchk(sender, new RoutedEventArgs());
            }
        }
        private void Canvas_MouseMv(object sender, MouseEventArgs e)
        {
            canvas.Children.Clear();
            DrawDocument();
            Point pos = e.GetPosition((Canvas)sender);
            if (openDocument.type == DrawingType.ORTHOGRAPHIC)
            {
                if (pointLock && !lassoing) pos = new Point(Helper.RoundTo(pos.X, pointSize), Helper.RoundTo(pos.Y, pointSize));
                else if (stepLock && !lassoing) pos = new Point(Helper.RoundTo(pos.X, (pointSize / openDocument.stepDivision)), Helper.RoundTo(pos.Y, (pointSize / openDocument.stepDivision)));

                ((StatusBarItem)this.FindName("CanvasLocation")).Content = "X: " + (pos.X / pointSize).ToString() + " Y: " + (pos.Y / pointSize).ToString();
            } else if (openDocument.type == DrawingType.ISOMETRIC)
            {
                if ((pointLock || stepLock) && !lassoing)
                {
                    Point tmp = Helper.Isometric.OrthoToIso(pos);
                    pos = Helper.Isometric.IsoToOrtho(new Point(Helper.RoundTo(tmp.X, pointLock ? pointSize : pointSize / openDocument.stepDivision), Helper.RoundTo(tmp.Y, pointLock ? pointSize : pointSize / openDocument.stepDivision)));
                }

                Point iso = Helper.Isometric.OrthoToIso(pos);
                ((StatusBarItem)this.FindName("CanvasLocation")).Content = "X: ~" + (iso.X / pointSize).ToString() + " Y: ~" + (iso.Y / pointSize).ToString();
            }
            if (drawLocator) canvas.Children.Add(new Ellipse()
            {
                Height = pointThickness + 4,
                Width = pointThickness + 4,
                Margin = new Thickness(pos.X - (pointThickness / 2) - 2, pos.Y - (pointThickness / 2) - 2, 0, 0),
                Fill = Brushes.Transparent,
                Stroke = Brushes.Black
            });
            if (currentAction == CanvasAction.LINE_PT2)
            {
                canvas.Children.Add(new Line()
                {
                    X1 = lastCanvasPoint.X,
                    Y1 = lastCanvasPoint.Y,
                    Stroke = Brushes.LightGray,
                    X2 = pos.X,
                    Y2 = pos.Y,
                    StrokeThickness = 2
                });
            }
            else if (currentAction == CanvasAction.RECT_PT2)
            {
                if (openDocument.type == DrawingType.ISOMETRIC)
                {
                    Point p1 = Helper.Isometric.OrthoToIso(lastCanvasPoint);
                    Point p2 = Helper.Isometric.OrthoToIso(pos);
                    Point rx = new Point(p1.X, p2.Y);
                    Point ry = new Point(p2.X, p1.Y);
                    p1 = Helper.Isometric.IsoToOrtho(p1);
                    p2 = Helper.Isometric.IsoToOrtho(p2);
                    rx = Helper.Isometric.IsoToOrtho(rx);
                    ry = Helper.Isometric.IsoToOrtho(ry);
                    canvas.Children.Add(new Line() { X1 = p1.X, Y1 = p1.Y, X2 = rx.X, Y2 = rx.Y, StrokeThickness = 2, Stroke = Brushes.LightGray });
                    canvas.Children.Add(new Line() { X1 = p1.X, Y1 = p1.Y, X2 = ry.X, Y2 = ry.Y, StrokeThickness = 2, Stroke = Brushes.LightGray });
                    canvas.Children.Add(new Line() { X1 = p2.X, Y1 = p2.Y, X2 = rx.X, Y2 = rx.Y, StrokeThickness = 2, Stroke = Brushes.LightGray });
                    canvas.Children.Add(new Line() { X1 = p2.X, Y1 = p2.Y, X2 = ry.X, Y2 = ry.Y, StrokeThickness = 2, Stroke = Brushes.LightGray });
                }
                else
                {
                    canvas.Children.Add(new Line() { X1 = lastCanvasPoint.X, Y1 = lastCanvasPoint.Y, X2 = lastCanvasPoint.X, Y2 = pos.Y, StrokeThickness = 2, Stroke = Brushes.LightGray });
                    canvas.Children.Add(new Line() { X1 = lastCanvasPoint.X, Y1 = lastCanvasPoint.Y, X2 = pos.X, Y2 = lastCanvasPoint.Y, StrokeThickness = 2, Stroke = Brushes.LightGray });
                    canvas.Children.Add(new Line() { X1 = pos.X, Y1 = pos.Y, X2 = lastCanvasPoint.X, Y2 = pos.Y, StrokeThickness = 2, Stroke = Brushes.LightGray });
                    canvas.Children.Add(new Line() { X1 = pos.X, Y1 = pos.Y, X2 = pos.X, Y2 = lastCanvasPoint.Y, StrokeThickness = 2, Stroke = Brushes.LightGray });
                }
            }
            else if (currentAction == CanvasAction.POLY_PT2)
            {
                double radius = Helper.Distance(auxCanvasPoint, lastCanvasPoint);
                int ngonSides = Math.Max(3, (int)Math.Round(360 / Math.Abs(Helper.AngleBetweenPoints(pos, lastCanvasPoint, auxCanvasPoint))));
                double intAng = 360 / ngonSides;
                double angOfs = Math.Abs(Helper.AngleBetweenPoints(lastCanvasPoint, new Point(auxCanvasPoint.X, auxCanvasPoint.Y + 1), auxCanvasPoint));
                for (int i = 0; i < ngonSides; i++)
                {
                    Point start = Helper.PointOnCircleCircumference(auxCanvasPoint, radius, (angOfs + (intAng * i)) % 360);
                    Point end = Helper.PointOnCircleCircumference(auxCanvasPoint, radius, (angOfs + (intAng * (i + 1))) % 360);
                    canvas.Children.Add(new Line() { X1 = start.X, Y1 = start.Y, X2 = end.X, Y2 = end.Y, StrokeThickness = 2, Stroke = Brushes.LightGray });
                }
            }
            else if (lassoing)
            {
                canvas.Children.Add(new Line() { X1 = lassoStart.X, Y1 = lassoStart.Y, X2 = lassoStart.X, Y2 = pos.Y, StrokeThickness = 2, Stroke = Brushes.LightBlue });
                canvas.Children.Add(new Line() { X1 = lassoStart.X, Y1 = lassoStart.Y, X2 = pos.X, Y2 = lassoStart.Y, StrokeThickness = 2, Stroke = Brushes.LightBlue });
                canvas.Children.Add(new Line() { X1 = pos.X, Y1 = pos.Y, X2 = lassoStart.X, Y2 = pos.Y, StrokeThickness = 2, Stroke = Brushes.LightBlue });
                canvas.Children.Add(new Line() { X1 = pos.X, Y1 = pos.Y, X2 = pos.X, Y2 = lassoStart.Y, StrokeThickness = 2, Stroke = Brushes.LightBlue });
            }
        }

        private void CanvasDragger_MouseDn(object sender, MouseButtonEventArgs e)
        {
            isPanning = true;
            lastDragPoint = e.GetPosition(canvasParent);
        }
        private void CanvasDragger_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isPanning = false;
        }
        private void CanvasDragger_MouseMv(object sender, MouseEventArgs e)
        {
            if (!isPanning) return;
            Point pos = e.GetPosition(canvasParent);
            yOffset += (int)Math.Round(pos.Y - lastDragPoint.Y);
            xOffset += (int)Math.Round(pos.X - lastDragPoint.X);
            lastDragPoint = pos;
            DrawDocument(true);
        }

        private void Colour_Click(object sender, RoutedEventArgs e)
        {
            //colourChooser.Visibility = Visibility.Visible;
            ColourChooser cc = new ColourChooser(colour.R, colour.G, colour.B);
            cc.ColourChosen += (object chooser, ColourChooser.ColourChooserClosedEventArgs col) => {
                colour = Color.FromRgb(col.red, col.green, col.blue);
            };
            cc.ShowDialog();
        }

        private void SelectionTransform(object sender, RoutedEventArgs e)
        {
            CoordinatePrompt triscp = new CoordinatePrompt("Transform", "Please enter transform distance");
            triscp.PromptSubmit += (object s, CoordinatePrompt.CoordinateEventArgs c) =>
            {
                SelectionTransform((float)c.x, (float)c.y);
            };
            triscp.ShowDialog();
        }
        private void SelectionTransform(float x, float y)
        {
            List<Edge> tmp = new List<Edge>();
            foreach (Edge edge in selection)
            {
                if (!openDocument.edges.Contains(edge))
                {
                    selection.Remove(edge);
                    continue;
                }
                Edge trEdge = edge;
                trEdge.x1 += (float)x;
                trEdge.y1 += (float)y;
                trEdge.x2 += (float)x;
                trEdge.y2 += (float)y;
                tmp.Add(trEdge);
                openDocument.edges.Remove(edge);
                openDocument.edges.Add(trEdge);
            }
            selection = tmp;
            DrawDocument();
        }
        private void SelectionDuplicate(object sender, RoutedEventArgs e)
        {
            CoordinatePrompt dsipxy = new CoordinatePrompt("Duplicate", "Please enter duplicate offset");
            dsipxy.PromptSubmit += (object s, CoordinatePrompt.CoordinateEventArgs c) =>
            {
                List<Edge> duplicatedEdges = new List<Edge>();
                foreach (Edge edge in selection)
                {
                    Edge dupEdge = edge;
                    duplicatedEdges.Add(dupEdge);
                    openDocument.edges.Add(dupEdge);
                }
                selection = duplicatedEdges;
                SelectionTransform((float)c.x, (float)c.y);
            };
            dsipxy.ShowDialog();
        }
        private void SelectionDelete(object sender, RoutedEventArgs e)
        {
            foreach (Edge edge in selection) openDocument.edges.Remove(edge);
            selection.Clear();
            DrawDocument();
        }
        private void SelectionRecolour(object sender, RoutedEventArgs e)
        {
            ColourChooser slrccp = new ColourChooser();
            slrccp.ColourChosen += (object s, ColourChooser.ColourChooserClosedEventArgs c) =>
            {
                List<Edge> tmp = new List<Edge>();
                foreach (Edge edge in selection)
                {
                    Edge rcEdge = edge;
                    rcEdge.r = c.red;
                    rcEdge.g = c.green;
                    rcEdge.b = c.blue;
                    tmp.Add(rcEdge);
                    openDocument.edges.Remove(edge);
                    openDocument.edges.Add(rcEdge);
                }
                selection = tmp;
                DrawDocument();
            };
            slrccp.ShowDialog();
        }

        private void CursorLockFree(object sender, RoutedEventArgs e)
        {
            stepLock = true;
            pointLock = true;
            GridLock_Click(sender, e);
        }
        private void CursorLockGrid(object sender, RoutedEventArgs e)
        {
            stepLock = true;
            pointLock = false;
            GridLock_Click(sender, e);
        }
        private void CursorLockStep(object sender, RoutedEventArgs e)
        {
            stepLock = false;
            pointLock = false;
            GridLock_Click(sender, e);
        }
        private void GridLock_Click(object sender, RoutedEventArgs e)
        {
            TextBlock glt = (TextBlock)this.FindName("GridLockToggleText");
            if (stepLock && pointLock)
            {
                glt.Text = "Free";
                ((RadioButton)FindName("DrawCursorFree")).IsChecked = true;
                stepLock = false;
                pointLock = false;
            } else if (stepLock)
            {
                glt.Text = "Grid";
                ((RadioButton)FindName("DrawCursorGrid")).IsChecked = true;
                pointLock = true;
            } else
            {
                glt.Text = "Step";
                ((RadioButton)FindName("DrawCursorStep")).IsChecked = true;
                stepLock = true;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!forceKill && MessageBox.Show("Unsaved changes will be lost. Proceed?", "Exit Application", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) e.Cancel = true;
            else Application.Current.Shutdown();
        }

        //Crash, CreateLogFile

        private void CreateLogFile(object sender, RoutedEventArgs e) { CreateLogFile(); }
        private void CreateLogFile()
        {
            try
            {
                DateTime logTime = DateTime.Now;
                string date = logTime.Day.ToString() + "-" + logTime.Month.ToString() + "-" + logTime.Year.ToString();
                string log = "ISODRAW DEBUG LOGFILE\n";
                log += "\nMainWindow Application instance properties are as follows:\n";
                this.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .ToList().ForEach(prop => {
                        if (prop.GetValue(this) == null) return;
                        log += prop.Name + ": " + prop.GetValue(this).ToString() + "\n";
                        });
                if (openDocument.edges.Count > 0) log += "\nOpen Document: " + openDocument.ToString();
                else log += "\nOpen Document was empty";
                log += "\nCurrent time: " + logTime.ToString();
                File.WriteAllText(Directory.GetCurrentDirectory() + @"\ISODRAW-" + date + ".txt", log);
                MessageBox.Show("A logfile has been dumped in the current working directory as 'ISODRAW-" + date + ".txt'.", "DEBUG/DUMPLOG", MessageBoxButton.OK, MessageBoxImage.Information);
            } catch
            {
                MessageBox.Show("An error occurred whilst trying to dump the logfile. Please check the file permissions.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AppInfo(object sender, RoutedEventArgs e) { AppInfo(); }
        private void AppInfo()
        {
            string info = "Name: " + Properties.Resources.AppNameVersion + "\nVersion: " + Properties.Resources.AppVersion + "\nAuthor: " + Properties.Resources.Author + "\nLang: en-uk";
            MessageBox.Show(info, "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void CopyrightInfo(object sender, RoutedEventArgs e) { CopyrightInfo(); }
        private void CopyrightInfo()
        {
            MessageBox.Show(Properties.Resources.Copyright, "Copyright", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Crash(object sender, RoutedEventArgs e) { Crash(); }
        private void Crash()
        {
            if (MessageBox.Show("This operation will cause all data open and unsaved edits to be lost, and the application will quit. Proceed with deliberate force crash?", "DEBUG/FCRASH", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            throw new Exception("User initiated crash");
        }

        private void Quit(object sender, RoutedEventArgs e) { Quit(); }
        private void Quit()
        {
            //if (MessageBox.Show("Unsaved changes will be lost. Proceed?", "Exit Application", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
            //Application.Current.Shutdown();
            this.Close();
        }
    }

    enum Tool
    {
        LINE,
        RECT,
        POLY,
        DEL,
        NONE
    }

    public enum DrawingType
    {
        ORTHOGRAPHIC,
        ISOMETRIC
    }

    enum CanvasAction
    {
        LINE_PT1,
        LINE_PT2,
        RECT_PT1,
        RECT_PT2,
        POLY_CTR,
        POLY_PT1,
        POLY_PT2,
        ERASE,
        IDLE
    }

    public struct Edge
    {
        public float x1, y1, x2, y2;
        public byte r, g, b;
    }

    class Document
    {
        public DrawingType type;
        public Size size;
        public int stepDivision = 2;
        public List<Edge> edges = new List<Edge>();
        public Document(DrawingType dtype, int width, int height)
        {
            if (height < 2 || width < 2) throw new ArgumentOutOfRangeException();
            type = dtype;
            size = new Size(width, height);
            edges = new List<Edge>();
        }
        public Document(string filedata)
        {
            try {
                if (filedata[0] != '$' || filedata[filedata.Length - 1] != '$') throw new FormatException("Document missing format identifier(s)");
                if (filedata[1] == 'I') type = DrawingType.ISOMETRIC;
                else if (filedata[1] == 'O') type = DrawingType.ORTHOGRAPHIC;
                else throw new FormatException("Invalid drawing type");
                if (filedata[2] != '/') throw new FormatException("Invalid document format");
                string sizeData = filedata.Substring(3, filedata.Substring(3).IndexOf('/'));
                size = new Size((double)int.Parse(sizeData.Substring(0, sizeData.IndexOf('#'))), (double)int.Parse(sizeData.Substring(sizeData.IndexOf('#') + 1)));
                string metaData = filedata.Substring(0, filedata.IndexOf("%")).Split('/')[2];
                stepDivision = int.Parse(metaData);
                string edgeData = filedata.Substring(filedata.IndexOf('%') + 1);
                edgeData = edgeData.Substring(0, edgeData.Length - 1);
                foreach (string edge in edgeData.Split('/'))
                {
                    string[] dat = edge.Split('&');
                    edges.Add(new Edge()
                    {
                        x1 = float.Parse(dat[0].Split('#')[0]),
                        x2 = float.Parse(dat[1].Split('#')[0]),
                        y1 = float.Parse(dat[0].Split('#')[1]),
                        y2 = float.Parse(dat[1].Split('#')[1]),
                        r = Helper.FromXhex(dat[2].Split('#')[0]),
                        g = Helper.FromXhex(dat[2].Split('#')[1]),
                        b = Helper.FromXhex(dat[2].Split('#')[2])
                    });
                }
            } catch
            {
                throw new FormatException("Invalid document format");
            }
        }
        public void SetEdge(Point start, Point end, Color color, bool state = true) { SetEdge(new Tuple<double, double>(start.X, start.Y), new Tuple<double, double>(end.X, end.Y), color, state); }
        public void SetEdge(Tuple<double, double> start, Tuple<double, double> end, Color color, bool state = true) { SetEdge(new Tuple<float, float>((float)start.Item1, (float)start.Item2), new Tuple<float, float>((float)end.Item1, (float)end.Item2), color, state); }
        public void SetEdge(Tuple<float, float> start, Tuple<float, float> end, Color color, bool state = true)
        {
            Edge tmp = new Edge()
            {
                x1 = start.Item1,
                x2 = end.Item1,
                y1 = start.Item2,
                y2 = end.Item2,
                r = color.R,
                g = color.G,
                b = color.B
            };
            if (state && !edges.Contains(tmp)) edges.Add(tmp);
            else if (!state) {
                int idex = -1;
                foreach (Edge edge in edges)
                {
                    idex++;
                    if (edge.x1 == tmp.x1 && edge.x2 == tmp.x2 && edge.y1 == tmp.y1 && edge.y2 == tmp.y2)
                    {
                        break;
                    }
                }
                if (idex >= 0) edges.RemoveAt(idex);
            }
        }
        public override string ToString()
        {
            if (edges.Count < 1) throw new Exception("Cannot save an empty document");
            string data = "$";
            data += type == DrawingType.ORTHOGRAPHIC ? "O/" : (type == DrawingType.ISOMETRIC ? "I/" : throw new Exception("Invalid drawing type"));
            data += ((int)Math.Round((double)size.Width)).ToString() + "#" + ((int)Math.Round((double)size.Height)).ToString() + "/";
            data += stepDivision.ToString() + "%";
            foreach (Edge edge in edges) data += (edge.x1.ToString() + "#" + edge.y1.ToString()) + "&" + (edge.x2.ToString() + "#" + edge.y2.ToString()) + "&" + (Helper.ToXhex(edge.r) + "#" + Helper.ToXhex(edge.g) + "#" + Helper.ToXhex(edge.b)) + "/";
            if (edges.Count > 0) data = data.Substring(0, data.Length - 1);
            return data + "$";
        }
    }

    static class Helper {
        public static class Isometric
        {
            public static readonly double eqHeight = Math.Sqrt(3) / 2;
            public static readonly double invertedEqHeight = 2 / Math.Sqrt(3);
            public static Point OrthoToIso(Point p)
            {
                //double y = invertedEqHeight * p.Y;
                //double x = p.X - (0.5 * y);
                //double x = invertedEqHeight * p.X;
                //double y = p.Y - (0.5 * /*p.X*/ p.Y); //FIX
                double x = invertedEqHeight * p.X;
                double y = p.Y - (0.5 * x);
                return new Point(x, y);
            }
            public static Point IsoToOrtho(Point p)
            {
                //double y = eqHeight * p.Y;
                //double x = p.X + (0.5 * y);
                //double x = eqHeight * p.X;
                //double y = p.Y + (0.5 * /*p.X*/ p.Y); //FIX
                double x = eqHeight * p.X;
                double y = (0.5 * p.X) + p.Y;
                return new Point(x, y);
            }
        }

        public static bool PointInPolygon(Point point, Point[] polygon) //Credit Saeed Amiri on StackOverflow
        {
            List<double> coef = polygon.Skip(1).Select((p, i) => (point.Y - polygon[i].Y) * (p.X - polygon[i].X) - (point.X - polygon[i].X) * (p.Y - polygon[i].Y)).ToList();
            if (coef.Any(p => p == 0))
                return true;
            for (int i = 1; i < coef.Count(); i++) if (coef[i] * coef[i - 1] < 0) return false;
            return true;
        }

        public static Point PointOnCircleCircumference(Point centre, double radius, double angle)
        {
            double radians = (angle * Math.PI) / 180;
            return new Point(centre.X + (radius * Math.Cos(radians)), centre.Y + (radius * Math.Sin(radians)));
        }
        public static double AngleBetweenPoints(Point a, Point b, Point vertex)
        {
            double aPrecalc = (a.X - vertex.X) / Distance(a, vertex);
            double aAngle = (180 * Math.Acos(aPrecalc)) / Math.PI;
            double bPrecalc = (b.X - vertex.X) / Distance(b, vertex);
            double bAngle = (180 * Math.Acos(bPrecalc)) / Math.PI;
            return bAngle - aAngle;
        }

        public static double Distance(Point a, Point b)
        {
            double w = Math.Abs(a.X - b.X);
            double h = Math.Abs(a.Y - b.Y);
            return Math.Sqrt((h * h) + (w * w));
        }

        public static int RoundTo(double value, int round)
        {
            return (int)Math.Round((double)(value / round)) * round;
        }
        public static double RoundTo(double value, double round)
        {
            return Math.Round((value / round)) * round;
        }

        public static string ToXhex(byte byt)
        {
            string[] xhex = { "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "a", "b", "c", "d", "e", "f" };
            string chra = xhex[byt / 16];
            string chrb = xhex[byt % 16];
            return chra + chrb;
        }
        public static byte FromXhex(string dat)
        {
            string[] xhexo = { "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "a", "b", "c", "d", "e", "f" };
            List<string> xhex = new List<string>(xhexo);
            return (byte)((xhex.IndexOf(dat[0] + "") * 16) + xhex.IndexOf(dat[1] + ""));
        }

        public static void RunCommand(string command)
        {
            System.Diagnostics.Process.Start("cmd.exe", "/C " + command);
        }

        public class ProgressAlert : Window
        {
            public ProgressBar progressBar = new ProgressBar()
            {
                Height = 20,
                Width = 200,
                Margin = new Thickness(10),
                IsIndeterminate = true
            };
            public void ShowProgress()
            {
                Title = Title ?? "Loading..." ;
                Width = 220;
                Height = 90;
                this.AddChild(progressBar);
                Show();
            }
        }
    }
}
