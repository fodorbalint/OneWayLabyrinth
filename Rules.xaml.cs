using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.Desktop;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp.Views.WPF;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Windows.Automation;
using System.Diagnostics.Eventing.Reader;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Logging;

namespace OneWayLabyrinth
{
    /// <summary>
    /// Interaction logic for Rules.xaml
    /// </summary>
    public partial class Rules : Window
    {
        string svgName;
        //int size;
        int xSize;
        int ySize;
        string grid;
        string[] ruleElements = new string[] { "liveEnd", "emptyField", "takenOrBorderField", "takenField", "takenLeftField", "takenRightField", "takenUpField", "takenDownField", "futureLineField", "forbiddenField" }; //"futureStartField", "futureEndField", "notCornerField", "countAreaPairStartField", "countAreaPairEndField", "countAreaPairBorderField", "countAreaImpairStartField", "countAreaImpairEndField", "countAreaImpairBorderField", "countAreaImpairDeterminedStartField", "countAreaImpairDeterminedEndField", "countAreaImpairDeterminedBorderField", "countAreaImpairDeterminedEntryField" };      
        string liveEnd, emptyField, takenOrBorderField, takenField, takenLeftField, takenRightField, takenUpField, takenDownField, futureLineField, futureLineField1, forbiddenField; // futureStartField, futureEndField, notCornerField, countAreaPairStartField, countAreaPairEndField, countAreaPairBorderField, countAreaImpairStartField, countAreaImpairEndField, countAreaImpairBorderField, countAreaImpairDeterminedStartField, countAreaImpairDeterminedEndField, countAreaImpairDeterminedBorderField, countAreaImpairDeterminedEntryField;
        int elementsInRow = 8;

        string newRule;
        int draggedElement = 0;
        SKElement draggedObj;
        double startMouseX, startMouseY;
        Thickness origMargin;
        List<int[]> takenCoordinates = new();
        List<int[]> forbiddenCoordinates = new();
        int[]? noCornerCoordinates = null;
        List<int[]> countAreaStartCoordinates = new();
        List<int[]> countAreaEndCoordinates = new();
        List<int[]> countAreaBorderCoordinates = new();
        int[]? directionField = null;
        int leftDir = 0, rightDir = 0;
        string relToLeft;
        int[]? arrowStart = null;
        int[]? arrowEnd = null;
        int childIndex = 0;
        List<string> activeRules = new();
        List<int[]> directions = new() { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } };
        public string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        bool futureLineMoving = false;
        List<int[]> futureLine; 
        int futureLineCurrentX = 0;
        int futureLineCurrentY = 0;
        int futureLinePrevDir = -1;

        public Rules()
        {
            InitializeComponent();
            //LoadDir();
            LoadSizeSetting();           
        }

        private void SaveSizeSetting()
        {
            xSize = int.Parse(XSize.Text);
            ySize = int.Parse(YSize.Text);
            /*size = int.Parse(AppliedSize.Text);*/

            string[] lines = new string[] { "ruleXSize: " + xSize, "ruleYSize: " + ySize };

            string linesStr = string.Join("\n", lines);

            string fileContent = File.ReadAllText(baseDir + "settings.txt");
            int pos = fileContent.IndexOf("ruleXSize");

            if (pos != -1)
            {
                File.WriteAllText(baseDir + "settings.txt", fileContent.Substring(0, pos) + linesStr);
            }
            else
            {
                File.WriteAllText(baseDir + "settings.txt", fileContent + linesStr);
            }
        }

        private void LoadSizeSetting()
        {
            string[] lines = File.ReadAllLines(baseDir + "settings.txt");

            if (lines.Length > 12)
            {
                /*string[] arr = lines[11].Split(": ");
                size = int.Parse(arr[1]);
                AppliedSize.Text = arr[1];*/

                string[] arr = lines[12].Split(": ");
                xSize = int.Parse(arr[1]);
                XSize.Text = arr[1];

                arr = lines[13].Split(": ");
                ySize = int.Parse(arr[1]);
                YSize.Text = arr[1];
            }
            else
            {
                /*size = 9;
                AppliedSize.Text = "9";*/

                xSize = 5;
                XSize.Text = "5";

                ySize = 5;
                YSize.Text = "5";
            }
        }


        // ----- Generate grid and field types -----


        private void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            CloseFutureLine();

            xSize = 1;
            ySize = 1;
            DrawGrid();

            string singleGrid = "<svg xmlns =\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\">\n\t<style>\n\t\tsvg { background-color: white; }\n\t</style>\n\t<!---->\n" + grid + "</svg>";
            if (!File.Exists(baseDir + "singleGrid.svg")) File.WriteAllText(baseDir + "singleGrid.svg", singleGrid);

            string color = "#ff0000";
            string opacity = "0.15";
            liveEnd = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\n" +
                "\t<path d=\"M 0.5 1 v -0.5\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" />";
            string content = singleGrid.Replace("<!---->", liveEnd);
            if (!File.Exists(baseDir + "liveEnd.svg")) File.WriteAllText(baseDir + "liveEnd.svg", content);

            emptyField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"#dddddd\" fill-opacity=\"1\" />";
            content = singleGrid.Replace("<!---->", emptyField);
            if (!File.Exists(baseDir + "emptyField.svg")) File.WriteAllText(baseDir + "emptyField.svg", content);

            color = "#000000";
            opacity = "1"; // originally 0.5
            takenOrBorderField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />";
            content = singleGrid.Replace("<!---->", takenOrBorderField);
            if (!File.Exists(baseDir + "takenOrBorderField.svg")) File.WriteAllText(baseDir + "takenOrBorderField.svg", content);

            color = "#ff0000";
            opacity = "0.15";
            takenField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\n" + "\t<path d=\"M 0.3 0.5 h 0.4 M 0.5 0.3 v 0.4\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" />";
            content = singleGrid.Replace("<!---->", takenField);
            if (!File.Exists(baseDir + "takenField.svg")) File.WriteAllText(baseDir + "takenField.svg", content);

            color = "#ff0000";
            opacity = "0.15";
            takenLeftField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\n" + "\t<path d=\"M 0.8 0.5 h -0.6 l 0.2 -0.2 l -0.2 0.2 l 0.2 0.2\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" />";
            content = singleGrid.Replace("<!---->", takenLeftField);
            if (!File.Exists(baseDir + "takenLeftField.svg")) File.WriteAllText(baseDir + "takenLeftField.svg", content);

            color = "#ff0000";
            opacity = "0.15";
            takenRightField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\n" + "\t<path d=\"M 0.2 0.5 h 0.6 l -0.2 -0.2 l 0.2 0.2 l -0.2 0.2\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" />";
            content = singleGrid.Replace("<!---->", takenRightField);
            if (!File.Exists(baseDir + "takenRightField.svg")) File.WriteAllText(baseDir + "takenRightField.svg", content);

            color = "#ff0000";
            opacity = "0.15";
            takenUpField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\n" + "\t<path d=\"M 0.5 0.8 v -0.6 l -0.2 0.2 l 0.2 -0.2 l 0.2 0.2\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" />";
            content = singleGrid.Replace("<!---->", takenUpField);
            if (!File.Exists(baseDir + "takenUpField.svg")) File.WriteAllText(baseDir + "takenUpField.svg", content);

            color = "#ff0000";
            opacity = "0.15";
            takenDownField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\n" + "\t<path d=\"M 0.5 0.2 v 0.6 l -0.2 -0.2 l 0.2 0.2 l 0.2 -0.2\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" />";
            content = singleGrid.Replace("<!---->", takenDownField);
            if (!File.Exists(baseDir + "takenDownField.svg")) File.WriteAllText(baseDir + "takenDownField.svg", content);

            futureLineField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"white\" fill-opacity=\"0\" />\n" + "\t<path d=\"M 0.3 0.5 h 0.4 M 0.5 0.3 v 0.4\" fill=\"white\" fill-opacity=\"0\" stroke=\"white\" stroke-width=\"0.1\" stroke-linecap=\"round\" />\n\t<path d=\"M 0.3 0.5 h 0.4 M 0.5 0.3 v 0.4\" fill=\"white\" fill-opacity=\"0\" stroke=\"blue\" stroke-width=\"0.05\" stroke-linecap=\"round\" />";
            futureLineField1 = "<!--4--><path d=\"M 0.3 0.5 h 0.4 M 0.5 0.3 v 0.4\" fill=\"white\" fill-opacity=\"0\" stroke=\"white\" stroke-width=\"0.1\" stroke-linecap=\"round\" />\n" + "<path d=\"M 0.3 0.5 h 0.4 M 0.5 0.3 v 0.4\" fill=\"white\" fill-opacity=\"0\" stroke=\"blue\" stroke-width=\"0.05\" stroke-linecap=\"round\" /><!--5-->";
            content = singleGrid.Replace("<!---->", futureLineField);
            if (!File.Exists(baseDir + "futureLineField.svg")) File.WriteAllText(baseDir + "futureLineField.svg", content);

            forbiddenField = "<path d=\"M 0.2 0.2 l 0.6 0.6 M 0.2 0.8 l 0.6 -0.6\" fill=\"white\" fill-opacity=\"0\" stroke=\"red\" stroke-width=\"0.05\" stroke-linecap=\"round\" />";
            content = singleGrid.Replace("<!---->", forbiddenField);
            if (!File.Exists(baseDir + "forbiddenField.svg")) File.WriteAllText(baseDir + "forbiddenField.svg", content);

            /*color = "#0000ff";
            opacity = "0.15";
            futureStartField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\n" +
                "\t<path d=\"M 0.2 0.2 v 0.6 v -0.3 h 0.6 l -0.2 -0.2 l 0.2 0.2 l -0.2 0.2\" fill=\"white\" fill-opacity=\"0\" stroke=\"blue\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", futureStartField);
            if (!File.Exists(baseDir + "futureStartField.svg")) File.WriteAllText(baseDir + "futureStartField.svg", content);

            color = "#0000ff";
            opacity = "0.15";

            futureEndField = "<path d=\"M 0 0 h 1 v 1 h -1 z\" fill=\"" + color + "\" fill-opacity=\"" + opacity + "\" />\n" +
                "\t<path d=\"M 0.2 0.5 h 0.6 l -0.2 -0.2 l 0.2 0.2 l -0.2 0.2 l 0.2 -0.2 v 0.3 v -0.6\" fill=\"white\" fill-opacity=\"0\" stroke=\"blue\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", futureEndField);
            if (!File.Exists(baseDir + "futureEndField.svg")) File.WriteAllText(baseDir + "futureEndField.svg", content);

            notCornerField = "<path d=\"M 0.3 0.2 v 0.5 h 0.5 h -0.2 a 0.3 0.3 0 0 0 -0.3 -0.3\" fill=\"white\" fill-opacity=\"0\" stroke=\"red\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" /><circle cx=\"0.42\" cy=\"0.58\" r=\"0.05\" fill=\"red\" fill-opacity=\"1\" />";
            content = singleGrid.Replace("<!---->", notCornerField);
            if (!File.Exists(baseDir + "notCornerField.svg")) File.WriteAllText(baseDir + "notCornerField.svg", content);*/

            /*countAreaPairStartField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#008000\" fill-opacity=\"0.25\" />\n\t<path d=\"M 0.3 0.3 v 0.4 v -0.2 h 0.4 l -0.13 -0.13 l 0.13 0.13 l -0.13 0.13\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", countAreaPairStartField);
            if (!File.Exists(baseDir + "countAreaPairStartField.svg")) File.WriteAllText(baseDir + "countAreaPairStartField.svg", content);

            countAreaPairEndField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#008000\" fill-opacity=\"0.25\" />\n\t<path d=\"M 0.3 0.5 h 0.4 l -0.13 -0.13 l 0.13 0.13 l -0.13 0.13 l 0.13 -0.13 v 0.2 v -0.4\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", countAreaPairEndField);
            if (!File.Exists(baseDir + "countAreaPairEndField.svg")) File.WriteAllText(baseDir + "countAreaPairEndField.svg", content);

            countAreaPairBorderField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#008000\" fill-opacity=\"0.25\" />";
            content = singleGrid.Replace("<!---->", countAreaPairBorderField);
            if (!File.Exists(baseDir + "countAreaPairBorderField.svg")) File.WriteAllText(baseDir + "countAreaPairBorderField.svg", content);

            countAreaImpairStartField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#808000\" fill-opacity=\"0.25\" />\n\t<path d=\"M 0.3 0.3 v 0.4 v -0.2 h 0.4 l -0.13 -0.13 l 0.13 0.13 l -0.13 0.13\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", countAreaImpairStartField);
            if (!File.Exists(baseDir + "countAreaImpairStartField.svg")) File.WriteAllText(baseDir + "countAreaImpairStartField.svg", content);

            countAreaImpairEndField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#808000\" fill-opacity=\"0.25\" />\n\t<path d=\"M 0.3 0.5 h 0.4 l -0.13 -0.13 l 0.13 0.13 l -0.13 0.13 l 0.13 -0.13 v 0.2 v -0.4\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", countAreaImpairEndField);
            if (!File.Exists(baseDir + "countAreaImpairEndField.svg")) File.WriteAllText(baseDir + "countAreaImpairEndField.svg", content);

            countAreaImpairBorderField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#808000\" fill-opacity=\"0.25\" />";
            content = singleGrid.Replace("<!---->", countAreaImpairBorderField);
            if (!File.Exists(baseDir + "countAreaImpairBorderField.svg")) File.WriteAllText(baseDir + "countAreaImpairBorderField.svg", content);

            countAreaImpairDeterminedStartField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#FF4000\" fill-opacity=\"0.25\" />\n\t<path d=\"M 0.3 0.3 v 0.4 v -0.2 h 0.4 l -0.13 -0.13 l 0.13 0.13 l -0.13 0.13\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", countAreaImpairDeterminedStartField);
            if (!File.Exists(baseDir + "countAreaImpairDeterminedStartField.svg")) File.WriteAllText(baseDir + "countAreaImpairDeterminedStartField.svg", content);

            countAreaImpairDeterminedEndField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#FF4000\" fill-opacity=\"0.25\" />\n\t<path d=\"M 0.3 0.5 h 0.4 l -0.13 -0.13 l 0.13 0.13 l -0.13 0.13 l 0.13 -0.13 v 0.2 v -0.4\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />";
            content = singleGrid.Replace("<!---->", countAreaImpairDeterminedEndField);
            if (!File.Exists(baseDir + "countAreaImpairDeterminedEndField.svg")) File.WriteAllText(baseDir + "countAreaImpairDeterminedEndField.svg", content);

            countAreaImpairDeterminedBorderField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#FF4000\" fill-opacity=\"0.25\" />";
            content = singleGrid.Replace("<!---->", countAreaImpairDeterminedBorderField);
            if (!File.Exists(baseDir + "countAreaImpairDeterminedBorderField.svg")) File.WriteAllText(baseDir + "countAreaImpairDeterminedBorderField.svg", content);

            countAreaImpairDeterminedEntryField = "<path d=\"M 0 0 h 1 v 1 h -1 v -0.8 h 0.2 v 0.6 h 0.6 v -0.6 h -0.8 z\" fill=\"#FF4000\" fill-opacity=\"0.25\" />\n" + "\t<path d=\"M 0.3 0.5 h 0.4 M 0.5 0.3 v 0.4\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" />";
            content = singleGrid.Replace("<!---->", countAreaImpairDeterminedEntryField);
            if (!File.Exists(baseDir + "countAreaImpairDeterminedEntryField.svg")) File.WriteAllText(baseDir + "countAreaImpairDeterminedEntryField.svg", content);*/

            if (RuleGrid.Children.Count == 1)
            {
                int i = 0;
                foreach (string ruleElement in ruleElements)
                {
                    i++;
                    SKElement el = new();
                    el.PaintSurface += SKElement_PaintSurface;
                    el.Tag = ruleElement;
                    el.Width = 40;
                    el.Height = 40;
                    el.HorizontalAlignment = HorizontalAlignment.Left;
                    el.VerticalAlignment = VerticalAlignment.Top;
                    el.Margin = new Thickness((i - 1) % elementsInRow * 50, (i - 1 - (i - 1) % elementsInRow) / elementsInRow * 50, 0, 0); 
                    RuleGrid.Children.Add(el);

                    Panel.SetZIndex(el, i);
                }
                Canvas.Margin = new Thickness(0, ((i - 1 - (i - 1) % elementsInRow) / elementsInRow + 1) * 50, 0, 0);
                NewRuleGrid.Height = 98 + ((RuleGrid.Children.Count - 2 - (RuleGrid.Children.Count - 2) % elementsInRow) / elementsInRow + 1) * 50 + ySize * 40;
            }

            svgName = "newRule.svg";

            xSize = int.Parse(XSize.Text);
            ySize = int.Parse(YSize.Text);

            Canvas.Width = xSize * 40;
            Canvas.Height = ySize * 40;

            ResetRule_Click(null, null);

            NewRuleGrid.Visibility = Visibility.Visible;
            SaveRule.Visibility = Visibility.Visible;
            ResetRule.Visibility = Visibility.Visible;
            Cancel.Visibility = Visibility.Visible;
        }

        private void ResetRule_Click(object sender, RoutedEventArgs e)
        {
            CloseFutureLine();
            SaveSizeSetting();

            Canvas.Width = xSize * 40;
            Canvas.Height = ySize * 40;

            DrawGrid();

            newRule = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 " + xSize + " " + ySize + "\">\n\t<style>\n\t\tsvg { background-color: white; }\n\t</style>\n\t<!--1-->\n\t<!--2-->\n" + grid + "\n\t<!--3-->\n</svg>";
            File.WriteAllText(baseDir + svgName, newRule);
            takenCoordinates = new();
            forbiddenCoordinates = new();
            noCornerCoordinates = null;
            countAreaStartCoordinates = new();
            countAreaEndCoordinates = new();
            countAreaBorderCoordinates = new();
            arrowStart = null;
            arrowEnd = null;

            NewRuleGrid.Height = 98 + ((RuleGrid.Children.Count - 2 - (RuleGrid.Children.Count - 2) % elementsInRow) / elementsInRow + 1) * 50 + ySize * 40; // the dragged element will otherwise stretch it on the bottom, no other solution have been found.
            /*RotateClockwise.IsChecked = false;
            RotateCounterClockwise.IsChecked = false;*/
            Canvas.InvalidateVisual();
        }

        private void DrawGrid()
        {
            grid = "";

            for (int i = 0; i <= xSize; i++)
            {
                float i2 = i;
                if (i == 0)
                {
                    i2 = i + 0.01f;
                }
                else if (i == xSize)
                {
                    i2 = i - 0.01f;
                }

                grid += "\t<path fill=\"transparent\" stroke=\"gray\" stroke-width=\"0.02\" d=\"M " + i2 + " 0 v " + ySize + "\" />\n";
            }
            for (int i = 0; i <= ySize; i++)
            {
                float i2 = i;
                if (i == 0)
                {
                    i2 = i + 0.01f;
                }
                else if (i == ySize)
                {
                    i2 = i - 0.01f;
                }

                grid += "\t<path fill=\"transparent\" stroke=\"gray\" stroke-width=\"0.02\" d=\"M 0 " + i2 + " h " + xSize + "\" />\n";
            }
        }

        private void XYSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveSizeSetting();

                Canvas.Width = xSize * 40;
                Canvas.Height = ySize * 40;

                ResizeGrid();
            }
        }

        private void AppliedSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveSizeSetting();
            }
        }

        private void ResizeGrid()
        {
            DrawGrid();

            int pos = newRule.IndexOf("viewBox=\"0 0 ");
            int lastPos = newRule.IndexOf("\"", pos + 13);
            string sizeStr = newRule.Substring(pos + 13, lastPos - pos - 13);
            newRule = newRule.Replace("viewBox=\"0 0 " + sizeStr, "viewBox=\"0 0 " + xSize + " " + ySize);

            T(newRule);

            int pos1 = newRule.IndexOf("<!--2-->");
            int pos2 = newRule.IndexOf("<!--3-->");
            newRule = newRule.Substring(0, pos1 + 8) + "\n" + grid + newRule.Substring(pos2);

            // remove fields that are now outside the area

            pos = 0;
            do
            {
                pos = newRule.IndexOf("<!-- ", pos);
                if (pos == -1) break;
                lastPos = newRule.IndexOf(" -->", pos);
                string areaCode = newRule.Substring(pos + 5, lastPos - pos - 5);

                string[] codes = areaCode.Split(" ");
                bool deleted = false;
                if (int.Parse(codes[0]) > xSize || int.Parse(codes[1]) > ySize)
                {
                    int nextPos = newRule.IndexOf("<!--", lastPos);
                    newRule = newRule.Substring(0, pos) + newRule.Substring(nextPos);
                    deleted = true;
                }
                if (!deleted) pos++;

            } while (true);

            File.WriteAllText(baseDir + svgName, newRule);

            NewRuleGrid.Height = 98 + ((RuleGrid.Children.Count - 2 - (RuleGrid.Children.Count - 2) % elementsInRow) / elementsInRow + 1) * 50 + ySize * 40;
            Canvas.InvalidateVisual();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            CloseFutureLine();
            NewRuleGrid.Visibility = Visibility.Collapsed;
            SaveRule.Visibility = Visibility.Collapsed;
            ResetRule.Visibility = Visibility.Collapsed;
            Cancel.Visibility = Visibility.Collapsed;
        }       


        // ----- Rule creation process -----


        private void RuleGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CloseFutureLine();

            T("Start mouse coordinates: " + e.GetPosition(RuleGrid).X + " " + e.GetPosition(RuleGrid).Y);
            T("NewRuleGrid height " + NewRuleGrid.ActualHeight);
            double x = e.GetPosition(RuleGrid).X;
            double y = e.GetPosition(RuleGrid).Y;
            startMouseX = x;
            startMouseY = y;
            int c = RuleGrid.Children.Count - 2;
            if (y <= ((c - c % elementsInRow) / elementsInRow + 1) * 50 + ySize * 40)
            {
                if (x % 50 >= 0 && x % 50 <= 40 && y % 50 >= 0 && y % 50 <= 40)
                {
                    draggedElement = (int)((x - x % 50) / 50 + (y - y % 50) / 50 * elementsInRow) + 1;
                }
            }

            if (draggedElement > c + 1)
            {
                draggedElement = 0;
                return;
            }

            draggedObj = (SKElement)RuleGrid.Children[draggedElement];
            T("draggedObj " + draggedObj);
            origMargin = draggedObj.Margin;
            Panel.SetZIndex(draggedObj, RuleGrid.Children.Count);
        }

        private void RuleGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (draggedElement == 0) return;
            double x = e.GetPosition(RuleGrid).X;
            double y = e.GetPosition(RuleGrid).Y;
            draggedObj.Margin = new Thickness(x - startMouseX + 50 * ((draggedElement - 1) % elementsInRow), y - startMouseY + 50 * ((draggedElement - 1 - (draggedElement - 1) % elementsInRow) / elementsInRow), 0, 0);
        }

        private void RuleGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (draggedElement == 0) return;
            draggedObj.Margin = origMargin;
            Panel.SetZIndex(draggedObj, draggedElement);
            draggedElement = 0;
        }

        private void RuleGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (draggedElement == 0) return;

            double x = e.GetPosition(Canvas).X;
            double y = e.GetPosition(Canvas).Y;

            draggedObj.Margin = origMargin;
            Panel.SetZIndex(draggedObj, draggedElement);
            double centerX = x - startMouseX + 20 + 50 * ((draggedElement - 1) % elementsInRow);
            double centerY = y - startMouseY + 20 + 50 * (draggedElement - 1 - (draggedElement - 1) % elementsInRow) / elementsInRow;

            if (centerX < 0 || centerY < 0)
            {
                draggedElement = 0;
                return;
            }

            int coordX = (int)(centerX - centerX % 40) / 40 + 1;
            int coordY = (int)(centerY - centerY % 40) / 40 + 1;

            T("coordX: " + coordX + " coordY: " + coordY);

            if (coordX <= xSize && coordY <= ySize)
            {
                T("End in table: " + coordX + " " + coordY);        
                
                /*if (draggedElement >= 13 && draggedElement <= 21)
                { // count area fields can only be placed on empty fields
                    foreach (int[] coord in takenCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY && coord[2] != 2)
                        {
                            draggedElement = 0;
                            return;
                        }
                    }

                    foreach (int[] coord in countAreaStartCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY)
                        {
                            draggedElement = 0;
                            return;
                        }
                    }

                    foreach (int[] coord in countAreaEndCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY)
                        {
                            draggedElement = 0;
                            return;
                        }
                    }

                    foreach (int[] coord in countAreaBorderCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY)
                        {
                            draggedElement = 0;
                            return;
                        }
                    }
                }

                if (draggedElement == 11) // no corner
                {
                    if (!(noCornerCoordinates is null))
                    {
                        T("Corner already exists.");
                        draggedElement = 0;
                        return;
                    }

                    if (coordX > 2 && coordX < xSize - 1 && coordY > 2)
                    {
                        T("Corner misplaced.");
                        draggedElement = 0;
                        return;
                    }

                    foreach (int[] coord in forbiddenCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY)
                        {
                            T("Corner cannot be forbidden.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    noCornerCoordinates = new int[] { coordX, coordY };
                }*/
                if (draggedElement == 10) // forbidden
                {
                    if (!(noCornerCoordinates is null) && coordX == noCornerCoordinates[0] && coordY == noCornerCoordinates[1])
                    {
                        T("Corner cannot be forbidden.");
                        draggedElement = 0;
                        return;
                    }

                    if (forbiddenCoordinates.Count == 2)
                    {
                        T("Too many forbidden.");
                        draggedElement = 0;
                        return;
                    }

                    foreach (int[] coord in forbiddenCoordinates)
                    {
                        if (coord[0] == coordX && coord[1] == coordY)
                        {
                            T("Found forbidden.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    if (forbiddenCoordinates.Count == 1 && !(Math.Abs(coordX - forbiddenCoordinates[0][0]) == 2 && coordY == forbiddenCoordinates[0][1] || Math.Abs(coordX - forbiddenCoordinates[0][0]) == 1 && Math.Abs(coordY - forbiddenCoordinates[0][1]) == 1))
                    {
                        T("Existing forbidden dislocated.");
                        draggedElement = 0;
                        return;
                    }

                    foreach (int[] coord in takenCoordinates)
                    {
                        if (coord[2] == 1 && !(Math.Abs(coordX - coord[0]) == 1 && coordY == coord[1] || coordY - coord[1] == -1 && coordX == coord[0]))
                        {
                            T("Live end dislocated.");
                            draggedElement = 0;
                            return;
                        }
                        else if (coordX == coord[0] && coordY == coord[1] && coord[2] != 2 && coord[2] != 9)
                        {
                            T("Only empty, undetermined or future start field should be marked forbidden.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    forbiddenCoordinates.Add(new int[] { coordX, coordY });
                }
                /*else if (draggedElement == 13 || draggedElement == 16 || draggedElement == 19) // count area start
                {
                    countAreaStartCoordinates.Add(new int[] { coordX, coordY, draggedElement - 12 });               
                    SaveMeta();
                }
                else if (draggedElement == 14 || draggedElement == 17 || draggedElement == 20) // count area end
                {   
                    countAreaEndCoordinates.Add(new int[] { coordX, coordY, draggedElement - 12 }); 
                    //CheckArrow(coordX, coordY);
                    SaveMeta();
                }
                else if (draggedElement == 15 || draggedElement == 18 || draggedElement == 21) // count area border
                {   
                    countAreaBorderCoordinates.Add(new int[] { coordX, coordY, draggedElement - 12 });                
                }
                else if (draggedElement == 22)
                {
                    countAreaBorderCoordinates.Add(new int[] { coordX, coordY, draggedElement - 12 });
                }*/
                else if (draggedElement == 9)
                {
                    futureLineMoving = true;                    
                    futureLineCurrentX = coordX;
                    futureLineCurrentY = coordY;
                    futureLine = new List<int[]> { new int[] { coordX, coordY } };                    
                }
                else {
                    if (draggedElement == 1)
                    {
                        if (forbiddenCoordinates.Count == 1)
                        {
                            if (!(Math.Abs(coordX - forbiddenCoordinates[0][0]) == 1 && coordY == forbiddenCoordinates[0][1] || coordY - forbiddenCoordinates[0][1] == 1 && coordX == forbiddenCoordinates[0][0]))
                            {
                                T("Forbidden field dislocated.");
                                draggedElement = 0;
                                return;
                            }
                        }
                        else if (forbiddenCoordinates.Count == 2)
                        {
                            if (forbiddenCoordinates[0][1] == forbiddenCoordinates[1][1] && !(coordX == (forbiddenCoordinates[0][0] + forbiddenCoordinates[1][0]) / 2 && coordY == forbiddenCoordinates[0][1]))
                            {
                                T("Two forbidden fields in a row dislocated.");
                                draggedElement = 0;
                                return;
                            }
                            else if (forbiddenCoordinates[0][1] > forbiddenCoordinates[1][1] && !(coordX == forbiddenCoordinates[1][0] && coordY == forbiddenCoordinates[0][1]) || forbiddenCoordinates[1][1] > forbiddenCoordinates[0][1] && !(coordX == forbiddenCoordinates[0][0] && coordY == forbiddenCoordinates[1][1]))
                            {
                                T("Two forbidden fields across dislocated.");
                                draggedElement = 0;
                                return;
                            }
                        }
                    }
                    else if (draggedElement != 2)
                    {
                        foreach (int[] coord in forbiddenCoordinates)
                        {
                            if (coordX == coord[0] && coordY == coord[1])
                            {
                                T("Only empty or undetermined should be marked forbidden.");
                                draggedElement = 0;
                                return;
                            }
                        }

                        // other than empty fields cannot be placed on countarea fields.
                        foreach (int[] coord in countAreaStartCoordinates)
                        {
                            if (coord[0] == coordX && coord[1] == coordY)
                            {
                                draggedElement = 0;
                                return;
                            }
                        }

                        foreach (int[] coord in countAreaEndCoordinates)
                        {
                            if (coord[0] == coordX && coord[1] == coordY)
                            {
                                draggedElement = 0;
                                return;
                            }
                        }

                        foreach (int[] coord in countAreaBorderCoordinates)
                        {
                            if (coord[0] == coordX && coord[1] == coordY)
                            {
                                draggedElement = 0;
                                return;
                            }
                        }
                    }

                    foreach (int[] coord in takenCoordinates)
                    {
                        if (draggedElement == 1 && coord[2] == 1)
                        {
                            T("Existing live end");
                            draggedElement = 0;
                            return;
                        }
                        else if (draggedElement == 9 && coord[2] == 9)
                        {
                            T("Existing future start");
                            M("Only one future start field can be added.");
                            draggedElement = 0;
                            return;
                        }
                        else if (draggedElement == 10 && coord[2] == 10)
                        {
                            T("Existing future end");
                            M("Only one future end field can be added.");
                            draggedElement = 0;
                            return;
                        }

                        if (draggedElement == 1 && coord[0] == coordX && coord[1] == coordY + 1)
                        {
                            T("Live end bottom taken.");
                            draggedElement = 0;
                            return;
                        }
                        else if (coord[0] == coordX && coord[1] == coordY)
                        {
                            T("Field taken.");
                            draggedElement = 0;
                            return;
                        }
                    }

                    takenCoordinates.Add(new int[] { coordX, coordY, draggedElement });
                }

                string addField = "";

                switch (draggedElement)
                {
                    case 1:
                        addField = liveEnd.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.5 1", "M " + (coordX - 0.5f) + " " + coordY) + "\n\t" +
                            liveEnd.Replace("M 0 0", "M " + (coordX - 1) + " " + coordY).Replace("M 0.5 1 v -0.5", "M " + (coordX - 0.5f) + " " + coordY + " v 0.5");
                        takenCoordinates.Add(new int[] { coordX, coordY + 1, 0 });
                        break;
                    case 2:
                        addField = emptyField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1));
                        break;
                    case 3:
                        addField = takenOrBorderField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1));
                        break;
                    case 4:
                        addField = takenField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.5", "M " + (coordX - 0.7f) + " " + (coordY - 0.5f)).Replace("M 0.5 0.3", "M " + (coordX - 0.5f) + " " + (coordY - 0.7f));
                        break;
                    case 5:
                        addField = takenLeftField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.8 0.5", "M " + (coordX - 0.2f) + " " + (coordY - 0.5f));
                        break;
                    case 6:
                        addField = takenRightField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.2 0.5", "M " + (coordX - 0.8f) + " " + (coordY - 0.5f));
                        break;
                    case 7:
                        addField = takenUpField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.5 0.8", "M " + (coordX - 0.5f) + " " + (coordY - 0.2f));
                        break;
                    case 8:
                        addField = takenDownField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.5 0.2", "M " + (coordX - 0.5f) + " " + (coordY - 0.8f));
                        break;
                    case 9:
                        addField = futureLineField1.Replace("M 0.3 0.5", "M " + (coordX - 0.7f) + " " + (coordY - 0.5f)).Replace("M 0.5 0.3", "M " + (coordX - 0.5f) + " " + (coordY - 0.7f));
                        break;
                    case 10:
                        addField = forbiddenField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.2 0.2", "M " + (coordX - 1 + 0.2f) + " " + (coordY - 1 + 0.2f)).Replace("M 0.2 0.8", "M " + (coordX - 1 + 0.2f) + " " + (coordY - 1 + 0.8f));
                        break;
                    /*case 9:
                        addField = futureStartField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.2 0.2", "M " + (coordX - 1 + 0.2f) + " " + (coordY - 1 + 0.2f));
                        break;
                    case 10:
                        addField = futureEndField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.2 0.5", "M " + (coordX - 1 + 0.2f) + " " + (coordY - 1 + 0.5f));
                        break;
                    case 11:
                        addField = notCornerField.Replace("M 0.3 0.2", "M " + (coordX - 0.7f) + " " + (coordY - 0.8f)).Replace("0.42", (coordX - 0.58f).ToString()).Replace("0.58", (coordY - 0.42f).ToString());
                        break;
                    case 12:                        
                    case 13:
                        addField = countAreaPairStartField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.3", "M " + (coordX - 1 + 0.3f) + " " + (coordY - 1 + 0.3f));
                        break;
                    case 14:
                        addField = countAreaPairEndField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.5", "M " + (coordX - 1 + 0.3f) + " " + (coordY - 1 + 0.5f));
                        break;
                    case 15:
                        addField = countAreaPairBorderField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1));
                        break;
                    case 16:
                        addField = countAreaImpairStartField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.3", "M " + (coordX - 1 + 0.3f) + " " + (coordY - 1 + 0.3f));
                        break;
                    case 17:
                        addField = countAreaImpairEndField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.5", "M " + (coordX - 1 + 0.3f) + " " + (coordY - 1 + 0.5f));
                        break;
                    case 18:
                        addField = countAreaImpairBorderField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1));
                        break;
                    case 19:
                        addField = countAreaImpairDeterminedStartField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.3", "M " + (coordX - 1 + 0.3f) + " " + (coordY - 1 + 0.3f));
                        break;
                    case 20:
                        addField = countAreaImpairDeterminedEndField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.5", "M " + (coordX - 1 + 0.3f) + " " + (coordY - 1 + 0.5f));
                        break;
                    case 21:
                        addField = countAreaImpairDeterminedBorderField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1));
                        break;
                    case 22:
                        addField = countAreaImpairDeterminedEntryField.Replace("M 0 0", "M " + (coordX - 1) + " " + (coordY - 1)).Replace("M 0.3 0.5", "M " + (coordX - 0.7f) + " " + (coordY - 0.5f)).Replace("M 0.5 0.3", "M " + (coordX - 0.5f) + " " + (coordY - 0.7f));
                        break;*/
                }

                if (draggedElement == 9)
                {
                    newRule = newRule.Replace("<!--3-->", "<!--3-->\n\t<!-- " + coordX + " " + coordY + " " + draggedElement + " -->\n\t" + addField);
                }
                else if (draggedElement == 10)
                {
                    newRule = newRule.Replace("<!--2-->", "<!-- " + coordX + " " + coordY + " " + draggedElement + " -->\n\t" + addField + "\n\t<!--2-->");
                }
                /*else if (draggedElement >= 13)
                {
                    newRule = newRule.Replace("<!--2-->", "<!-- " + coordX + " " + coordY + " " + draggedElement + " -->\n\t" + addField + "\n\t<!--2-->");
                }*/
                else
                {
                    newRule = newRule.Replace("<!--1-->", "<!-- " + coordX + " " + coordY + " " + draggedElement + " -->\n\t" + addField + "\n\t<!--1-->");
                }

                File.WriteAllText(baseDir + svgName, newRule);
                Canvas.InvalidateVisual();
            }

            draggedElement = 0;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) // with normal KeyDown, event does not fire after calling FocusButton.Focus();
        {
            if (e.Key == Key.Enter)
            {
                CloseFutureLine();
                return;
            }

            directions = new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } };
            int index = -1;

            if (futureLineMoving)
            {
                if (e.Key == Key.Down)
                {
                    index = 0;
                }
                else if (e.Key == Key.Right)
                {
                    index = 1;
                }
                else if (e.Key == Key.Up)
                {
                    index = 2;
                }
                else if (e.Key == Key.Left)
                {
                    index = 3;
                }

                if (futureLinePrevDir != -1 && index != futureLinePrevDir && Math.Abs(index - futureLinePrevDir) == 2)
                {
                    return;
                }
                else futureLinePrevDir = index;

                if (index != -1)
                {
                    futureLineCurrentX += directions[index][0];
                    futureLineCurrentY += directions[index][1];

                    futureLine.Add(new int[] { futureLineCurrentX, futureLineCurrentY });

                    DrawPath();
                }
            }            
        }

        private void DrawPath()
        {            
            int startX = futureLine[0][0];
            int startY = futureLine[0][1];
            float posX = startX - 0.5f;
            float posY = startY - 0.5f;
            string path = "M " + posX + " " + posY + "\r\n";
            int newX = 0;
            int newY = 0;

            for (int i = 1; i < futureLine.Count; i++)
            {
                int[] field = futureLine[i];
                newX = field[0];
                newY = field[1];

                float prevX = posX;
                float prevY = posY;

                if(startY == newY)

                {
                    posX = (float)(startX + newX) / 2 - 0.5f;
                    posY = newY - 0.5f;
                }

                else
                {
                    posY = (float)(startY + newY) / 2 - 0.5f;
                    posX = newX - 0.5f;
                }

                if (prevX == posX || prevY == posY)
                {
                    path += "L " + posX + " " + posY + "\r\n";
                }
                else
                {
                    int dir = 0; //counter-clockwise

                    if (posX > prevX && posY > prevY)
                    {
                        if (startX - 0.5f == posX)
                        {
                            dir = 1;
                        }
                    }
                    if (posX < prevX && posY < prevY)
                    {
                        if (startX - 0.5f == posX)
                        {
                            dir = 1;
                        }
                    }
                    if (posX > prevX && posY < prevY)
                    {
                        if (startY - 0.5f == posY)
                        {
                            dir = 1;
                        }
                    }
                    if (posX < prevX && posY > prevY)
                    {
                        if (startY - 0.5f == posY)
                        {
                            dir = 1;
                        }
                    }
                    path += "A 0.5 0.5 0 0 " + dir + " " + posX + " " + posY + "\r\n";
                }

                startX = newX;
                startY = newY;
            }

            path += "L " + (newX - 0.5f) + " " + (newY - 0.5f) + "\r\n";

            int startPos = newRule.IndexOf("<!--4-->");
            int endPos = newRule.IndexOf("<!--5-->");
            newRule = newRule.Substring(0, startPos) + "<!--4--><path d=\"" + path + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"white\" stroke-width=\"0.1\" stroke-linecap=\"round\" />\n" + "<path d=\"" + path + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"blue\" stroke-width=\"0.05\" stroke-linecap=\"round\" />" + newRule.Substring(endPos);

            File.WriteAllText(baseDir + svgName, newRule);
            Canvas.InvalidateVisual();
        }

        private void CloseFutureLine()
        {
            futureLineMoving = false;
            futureLinePrevDir = -1;

            if (newRule != null)
            {
                newRule = newRule.Replace("<!--4-->", "").Replace("<!--5-->", "");
                File.WriteAllText(baseDir + svgName, newRule);
            }
        }

        /*private void CheckArrow(int coordX, int coordY)
        {
            // find taken field next to the count area end, find second taken field and mark the direction.
            int dir = -1;
            int[] nextToField, nextToLeftField, nextToRightField, nextToLeft2Field, nextToRight2Field;

            if (!(countAreaStartCoordinates is null))
            {
                int cStartX = countAreaStartCoordinates[0];
                int cStartY = countAreaStartCoordinates[1];

                if (coordX == cStartX)
                {
                    T((coordY - cStartY) / Math.Abs(coordY - cStartY));
                    dir = FindDirection(0, (coordY - cStartY) / Math.Abs(coordY - cStartY));
                }
                else if (coordY == cStartY)
                {
                    dir = FindDirection((coordX - cStartX) / Math.Abs(coordX - cStartX), 0);
                }
                else if (countAreaBorderCoordinates.Count != 0)
                {
                    foreach (int[] coord in countAreaBorderCoordinates)
                    {
                        if (coordX == coord[0] && Math.Abs(coordY - coord[1]) == 1 || coordY == coord[1] && Math.Abs(coordX - coord[0]) == 1)
                        {
                            dir = FindDirection(coordX - coord[0], coordY - coord[1]);
                        }
                    }
                }
            }
            else if (countAreaBorderCoordinates.Count != 0)
            {
                foreach (int[] coord in countAreaBorderCoordinates)
                {
                    if (coordX == coord[0] && Math.Abs(coordY - coord[1]) == 1 || coordY == coord[1] && Math.Abs(coordX - coord[0]) == 1)
                    {
                        dir = FindDirection(coordX - coord[0], coordY - coord[1]);
                    }
                }

            }

            if (dir != -1)
            {
                int leftDir = dir == 3 ? 0 : dir + 1;
                int rightDir = dir == 0 ? 3 : dir - 1;

                nextToField = new int[] { coordX + directions[dir][0], coordY + directions[dir][1] };
                nextToLeftField = new int[] { nextToField[0] + directions[leftDir][0], nextToField[1] + directions[leftDir][1] };
                nextToRightField = new int[] { nextToField[0] + directions[rightDir][0], nextToField[1] + directions[rightDir][1] };
                nextToLeft2Field = new int[] { nextToField[0] + 2 * directions[leftDir][0], nextToField[1] + 2 * directions[leftDir][1] };
                nextToRight2Field = new int[] { nextToField[0] + 2 * directions[rightDir][0], nextToField[1] + 2 * directions[rightDir][1] };

                int pos, endPos;
                foreach (int[] coord in takenCoordinates)
                {
                    if (coord[2] == 3 && coord[0] == nextToLeftField[0] && coord[1] == nextToLeftField[1])
                    {
                        foreach (int[] coord2 in takenCoordinates)
                        {
                            if (coord2[2] == 3 && coord2[0] == nextToField[0] && coord2[1] == nextToField[1])
                            {
                                pos = newRule.IndexOf("<!-- " + coord[0] + " " + coord[1] + " 3 -->");
                                pos = newRule.IndexOf("<path", pos);
                                pos = newRule.IndexOf("<path", pos + 1);
                                endPos = newRule.IndexOf("/>", pos);
                                newRule = newRule.Substring(0, pos - 3) + newRule.Substring(endPos + 2);

                                pos = newRule.IndexOf("<!-- " + coord2[0] + " " + coord2[1] + " 3 -->");
                                pos = newRule.IndexOf("<path", pos);
                                pos = newRule.IndexOf("<path", pos + 1);
                                endPos = newRule.IndexOf("/>", pos);
                                newRule = newRule.Substring(0, pos - 3) + newRule.Substring(endPos + 2);

                                if (CircleDirectionLeft.IsChecked == true) // arrow from middle to left
                                {
                                    newRule = newRule.Replace("</svg>", "\n\t<!-- " + nextToField[0] + " " + nextToField[1] + " " + nextToLeftField[0] + " " + nextToLeftField[1] + " 15 -->\n\t<path d=\"" + DrawArrow(nextToField[0], nextToField[1], nextToLeftField[0], nextToLeftField[1]) + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />\n</svg>");
                                }
                                else
                                {
                                    newRule = newRule.Replace("</svg>", "\n\t<!-- " + nextToLeftField[0] + " " + nextToLeftField[1] + " " + nextToField[0] + " " + nextToField[1] + " 15 -->\n\t<path d=\"" + DrawArrow(nextToLeftField[0], nextToLeftField[1], nextToField[0], nextToField[1]) + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />\n</svg>");
                                }
                                break;
                            }
                            else if (coord2[2] == 3 && coord2[0] == nextToLeft2Field[0] && coord2[1] == nextToLeft2Field[1])
                            {
                                pos = newRule.IndexOf("<!-- " + coord[0] + " " + coord[1] + " 3 -->");
                                pos = newRule.IndexOf("<path", pos);
                                pos = newRule.IndexOf("<path", pos + 1);
                                endPos = newRule.IndexOf("/>", pos);
                                newRule = newRule.Substring(0, pos - 3) + newRule.Substring(endPos + 2);

                                pos = newRule.IndexOf("<!-- " + coord2[0] + " " + coord2[1] + " 3 -->");
                                pos = newRule.IndexOf("<path", pos);
                                pos = newRule.IndexOf("<path", pos + 1);
                                endPos = newRule.IndexOf("/>", pos);
                                newRule = newRule.Substring(0, pos - 3) + newRule.Substring(endPos + 2);

                                if (CircleDirectionLeft.IsChecked == true) // arrow from middle to left
                                {
                                    newRule = newRule.Replace("</svg>", "\n\t<!-- " + nextToLeftField[0] + " " + nextToLeftField[1] + " " + nextToLeft2Field[0] + " " + nextToLeft2Field[1] + " 15 -->\n\t<path d=\"" + DrawArrow(nextToLeftField[0], nextToLeftField[1], nextToLeft2Field[0], nextToLeft2Field[1]) + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />\n</svg>");
                                }
                                else
                                {
                                    newRule = newRule.Replace("</svg>", "\n\t<!-- " + nextToLeft2Field[0] + " " + nextToLeft2Field[1] + " " + nextToLeftField[0] + " " + nextToLeftField[1] + " 15 -->\n\t<path d=\"" + DrawArrow(nextToLeft2Field[0], nextToLeft2Field[1], nextToLeftField[0], nextToLeftField[1]) + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />\n</svg>");
                                }
                                break;
                            }
                        }
                    }
                    if (coord[2] == 3 && coord[0] == nextToRightField[0] && coord[1] == nextToRightField[1])
                    {
                        //straightFound = true;

                        foreach (int[] coord2 in takenCoordinates)
                        {
                            if (coord2[2] == 3 && coord2[0] == nextToField[0] && coord2[1] == nextToField[1])
                            {
                                pos = newRule.IndexOf("<!-- " + coord[0] + " " + coord[1] + " 3 -->");
                                pos = newRule.IndexOf("<path", pos);
                                pos = newRule.IndexOf("<path", pos + 1);
                                endPos = newRule.IndexOf("/>", pos);
                                newRule = newRule.Substring(0, pos - 3) + newRule.Substring(endPos + 2);

                                pos = newRule.IndexOf("<!-- " + coord2[0] + " " + coord2[1] + " 3 -->");
                                pos = newRule.IndexOf("<path", pos);
                                pos = newRule.IndexOf("<path", pos + 1);
                                endPos = newRule.IndexOf("/>", pos);
                                newRule = newRule.Substring(0, pos - 3) + newRule.Substring(endPos + 2);

                                if (CircleDirectionLeft.IsChecked == true) // arrow from middle to left
                                {
                                    newRule = newRule.Replace("</svg>", "\n\t<!-- " + nextToRightField[0] + " " + nextToRightField[1] + " " + nextToField[0] + " " + nextToField[1] + " 15 -->\n\t<path d=\"" + DrawArrow(nextToRightField[0], nextToRightField[1], nextToField[0], nextToField[1]) + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />\n</svg>");
                                }
                                else
                                {
                                    newRule = newRule.Replace("</svg>", "\n\t<!-- " + nextToField[0] + " " + nextToField[1] + " " + nextToRightField[0] + " " + nextToRightField[1] + " 15 -->\n\t<path d=\"" + DrawArrow(nextToField[0], nextToField[1], nextToRightField[0], nextToRightField[1]) + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />\n</svg>");
                                }
                                break;
                            }
                            else if (coord2[2] == 3 && coord2[0] == nextToRight2Field[0] && coord2[1] == nextToRight2Field[1])
                            {
                                pos = newRule.IndexOf("<!-- " + coord[0] + " " + coord[1] + " 3 -->");
                                pos = newRule.IndexOf("<path", pos);
                                pos = newRule.IndexOf("<path", pos + 1);
                                endPos = newRule.IndexOf("/>", pos);
                                newRule = newRule.Substring(0, pos - 3) + newRule.Substring(endPos + 2);

                                pos = newRule.IndexOf("<!-- " + coord2[0] + " " + coord2[1] + " 3 -->");
                                pos = newRule.IndexOf("<path", pos);
                                pos = newRule.IndexOf("<path", pos + 1);
                                endPos = newRule.IndexOf("/>", pos);
                                newRule = newRule.Substring(0, pos - 3) + newRule.Substring(endPos + 2);

                                if (CircleDirectionLeft.IsChecked == true) // arrow from middle to left
                                {
                                    newRule = newRule.Replace("</svg>", "\n\t<!-- " + nextToRight2Field[0] + " " + nextToRight2Field[1] + " " + nextToRightField[0] + " " + nextToRightField[1] + " 15 -->\n\t<path d=\"" + DrawArrow(nextToRight2Field[0], nextToRight2Field[1], nextToRightField[0], nextToRightField[1]) + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />\n</svg>");
                                }
                                else
                                {
                                    newRule = newRule.Replace("</svg>", "\n\t<!-- " + nextToRightField[0] + " " + nextToRightField[1] + " " + nextToRight2Field[0] + " " + nextToRight2Field[1] + " 15 -->\n\t<path d=\"" + DrawArrow(nextToRightField[0], nextToRightField[1], nextToRight2Field[0], nextToRight2Field[1]) + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />\n</svg>");
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        private string DrawArrow(int startX, int startY, int endX, int endY)
        {
            arrowStart = new int[] { startX, startY };
            arrowEnd = new int[] { endX, endY };

            if (startY == endY)
            {
                if (startX < endX)
                {
                    return "M " + (startX - 0.67) + " " + (startY - 0.5) + " h 1.33 l -0.25 -0.25 m 0.25 0.25 l -0.25 0.25";
                }
                else
                {
                    return "M " + (startX - 0.33) + " " + (startY - 0.5) + " h -1.33 l 0.25 -0.25 m -0.25 0.25 l 0.25 0.25";
                }
            }
            else
            {
                if (startY < endY)
                {
                    return "M " + (startX - 0.5) + " " + (startY - 0.67) + " v 1.33 l -0.25 -0.25 m 0.25 0.25 l 0.25 -0.25";
                }
                else
                {
                    return "M " + (startX - 0.5) + " " + (startY - 0.33) + " v -1.33 l -0.25 0.25 m 0.25 -0.25 l 0.25 0.25";
                }
            }
        }*/

        private void SaveRule_Click(object sender, RoutedEventArgs e)
        {
            CloseFutureLine();
            SaveSizeSetting();

            /*if (countAreaStartCoordinates.Count != countAreaEndCoordinates.Count)
            {
                M("Count area start and end fields are not equal.");
                return;
            }

            if (!ValidateCountAreaFields())
            {
                return;
            }*/

            /*if (xSize > size || ySize > size)
            {
                M("The rule is larger than the applied size.");
                return;
            }*/
            if (takenCoordinates.Count == 0)
            {
                M("Empty rule not saved.");
                return;
            }
            if (RuleName.Text.Trim() == "")
            {
                M("Enter filename.");
                return;
            }
            /*if (size % 2 == 0 || size < 5 || size > 21)
            {
                M("Applied size must be impair and between 5 and 21.");
                return;
            }*/
            if (newRule.IndexOf("1 -->") == -1)
            {
                M("No live end added.");
                return;
            }
            /*if (newRule.IndexOf("12 -->") == -1)
            {
                M("No forbidden fields added.");
                return;
            }*/
            if (newRule.IndexOf("2 -->") == -1 && newRule.IndexOf("3 -->") == -1 && newRule.IndexOf("4 -->") == -1 && newRule.IndexOf("5 -->") == -1 && newRule.IndexOf("6 -->") == -1 && newRule.IndexOf("7 -->") == -1 && newRule.IndexOf("8 -->") == -1 && newRule.IndexOf("9 -->") == -1 && newRule.IndexOf("10 -->") == -1)
            {
                M("No conditions are added.");
                return;
            }
            /*if (!Directory.Exists(baseDir + "rules/" + size))
            {
                Directory.CreateDirectory(baseDir + "rules/" + size);
            }*/

            string fileName = RuleName.Text + ".svg";
            if (RuleName.Text.Length > 4)
            {
                fileName = RuleName.Text.Substring(RuleName.Text.Length - 4) == ".svg" ? RuleName.Text : RuleName.Text + ".svg";
            }

            string fullName = fileName;
            if (File.Exists(baseDir + fullName))
            {
                int i = 1;
                while (File.Exists(fullName.Replace(".svg", "_" + i + ".svg")))
                {
                    i++;
                }
                fullName = fullName.Replace(".svg", "_" + i + ".svg");
            }

            T("Save: " + fullName);
            File.Copy(baseDir + "newRule.svg", baseDir + fullName);
            ResetRule_Click(null, null);
            //LoadDir();
        }

        private bool ValidateCountAreaFields()
        {
            // check if there is a matching type end field. If the distance of the start and end fields are larger than 2 in line, find border fields from closest to the start field up to the end field.
            // check for a directional taken field next to or across the end field.
            foreach (int[] coord in countAreaStartCoordinates)
            {
                int sx = coord[0];
                int sy = coord[1];
                int stype = coord[2];

                bool foundEnd = false;
                bool foundDirTaken = false;

                int entryCount = 0;

                foreach (int[] coord1 in countAreaEndCoordinates)
                {
                    int ex = coord1[0];
                    int ey = coord1[1];
                    int etype = coord1[2];

                    if (etype == stype + 1)
                    {
                        bool correctEnd = false;

                        int distX = 0, distY = 0;
                        if (Math.Abs(ex - sx) == 1 && Math.Abs(ey - sy) == 0 || Math.Abs(ey - sy) == 1 && Math.Abs(ex - sx) == 0)
                        {
                            correctEnd = true;
                            distX = sx - ex;
                            distY = sy - ey;
                        }
                        else
                        {
                            int dist = Math.Abs(sx - ex) + Math.Abs(sy - ey);
                            correctEnd = true;
                            for (int i = 1; i < dist; i++)
                            {
                                bool foundDist = false;
                                foreach (int[] coord2 in countAreaBorderCoordinates)
                                {
                                    int bx = coord2[0];
                                    int by = coord2[1];
                                    int btype = coord2[2];

                                    if ((btype == stype + 2 || btype == stype + 3) && (Math.Abs(sx - bx) + Math.Abs(sy - by) == i))
                                    {
                                        foundDist = true;
                                        if (i == dist - 1)
                                        {
                                            distX = bx - ex;
                                            distY = by - ey;
                                        }

                                        if (btype == stype + 3)
                                        {
                                            if ((bx + by) % 2 == (sx + sy) % 2)
                                            {
                                                M("Entry point cannot be of same type as the count area start field.");
                                                return false;
                                            }
                                            entryCount++;
                                        }
                                        break;
                                    }
                                }

                                if (!foundDist)
                                {
                                    correctEnd = false; // the end is not for the same area, or border fields are missing.
                                    break;
                                }
                            }
                        }

                        if (!correctEnd)
                        {
                            continue;
                        }
                        else
                        {
                            if (entryCount > 1)
                            {
                                M("There cannot be more than one entry point.");
                                return false;
                            }
                            foundEnd = true;
                        }

                        if (!CheckTakenOrBorderNearEnd(ex, ey, distX, distY))
                        {
                            // directional field missing or the two different areas have start and end fields next to each other. In this case, the end is incorrect.
                            foundDirTaken = false;
                            continue;
                        }
                        else
                        {
                            foundDirTaken = true;
                            break;
                        }
                    }
                }

                if (!foundEnd)
                {
                    M("Count area end or border fields missing.");
                    return false;
                }
                if (!foundDirTaken)
                {
                    M("There has to be a directional taken field next to the count area end.");
                    return false;
                }
            }

            return true;
        }

        private bool CheckTakenOrBorderNearEnd(int countAreaEndX, int countAreaEndY, int distX, int distY)
        {
            int middleWallX, middleWallY, leftAcrossWallX, leftAcrossWallY, rightAcrossWallX, rightAcrossWallY;

            middleWallX = countAreaEndX - distX;
            middleWallY = countAreaEndY - distY;

            int dir = FindDirection(distX, distY);
            int leftDir = (dir == 0) ? 3 : dir - 1;
            int rightDir = (dir == 3) ? 0 : dir + 1;

            leftAcrossWallX = middleWallX + directions[leftDir][0];
            leftAcrossWallY = middleWallY + directions[leftDir][1];
            rightAcrossWallX = middleWallX + directions[rightDir][0];
            rightAcrossWallY = middleWallY + directions[rightDir][1];

            foreach (int[] takenField in takenCoordinates)
            {
                if (takenField[0] == middleWallX && takenField[1] == middleWallY || takenField[0] == leftAcrossWallX && takenField[1] == leftAcrossWallY || takenField[0] == rightAcrossWallX && takenField[1] == rightAcrossWallY)
                {
                    T(takenField[0] + " " + takenField[1] + " " + takenField[2] + " " + (dir % 2));
                    if (dir % 2 == 1 && (takenField[2] == 6 || takenField[2] == 7) || dir % 2 == 0 && (takenField[2] == 4 || takenField[2] == 5)) // left and right field up and down
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool GetCircleDirection(int countAreaEndX, int countAreaEndY, int distX, int distY, List<int[]> takenFields)
        { // sets circle direction
            int middleWallX, middleWallY, leftAcrossWallX, leftAcrossWallY, rightAcrossWallX, rightAcrossWallY;

            middleWallX = countAreaEndX - distX;
            middleWallY = countAreaEndY - distY;

            int dir = FindDirection(distX, distY);
            leftDir = (dir == 0) ? 3 : dir - 1;
            rightDir = (dir == 3) ? 0 : dir + 1;

            leftAcrossWallX = middleWallX + directions[leftDir][0];
            leftAcrossWallY = middleWallY + directions[leftDir][1];
            rightAcrossWallX = middleWallX + directions[rightDir][0];
            rightAcrossWallY = middleWallY + directions[rightDir][1];

            foreach (int[] takenField in takenFields)
            {
                if (takenField[0] == middleWallX && takenField[1] == middleWallY || takenField[0] == leftAcrossWallX && takenField[1] == leftAcrossWallY || takenField[0] == rightAcrossWallX && takenField[1] == rightAcrossWallY)
                {
                    switch (dir)
                    {
                        case 0:
                            if (takenField[2] == 4)
                            {
                                directionField = new int[] { takenField[0], takenField[1] };
                                return true;
                            }
                            else if (takenField[2] == 5)
                            {
                                directionField = new int[] { takenField[0], takenField[1] };
                                return false;
                            }
                            break;
                        case 1:
                            if (takenField[2] == 6)
                            {
                                directionField = new int[] { takenField[0], takenField[1] };
                                return false;
                            }
                            else if (takenField[2] == 7)
                            {
                                directionField = new int[] { takenField[0], takenField[1] };
                                return true;
                            }
                            break;
                        case 2:
                            if (takenField[2] == 4)
                            {
                                directionField = new int[] { takenField[0], takenField[1] };
                                return false;
                            }
                            else if (takenField[2] == 5)
                            {
                                directionField = new int[] { takenField[0], takenField[1] };
                                return true;
                            }
                            break;
                        case 3:
                            if (takenField[2] == 6)
                            {
                                directionField = new int[] { takenField[0], takenField[1] };
                                return true;
                            }
                            else if (takenField[2] == 7)
                            {
                                directionField = new int[] { takenField[0], takenField[1] };
                                return false;
                            }
                            break;
                    } 
                }
            }

            return true;
        }


        // ----- Generate code -----


        /*private void LoadDir()
        {
            MainGrid.Children.RemoveRange(0, childIndex);
            childIndex = 0;
            string[] listOfSizes = Directory.GetDirectories(baseDir + "rules");            
            int yTotalPos = 0;
            string codeString = "namespace OneWayLabyrinth\n{\n\tusing System.Collections.Generic;\n\n\tpublic partial class Path\n\t{\n\t\tint directionFieldIndex = 0;\n\t\tList<string> activeRules;\n\t\tList<List<int[]>> activeRulesForbiddenFields;\n\t\tList<int[]> activeRuleSizes;\n\t\tList<int[]> startForbiddenFields;\n[conditionVariablesDeclare]\n\t\tpublic void RunRules()\n\t\t{" +
                "\n\t\t\tactiveRules = new();\n\t\t\tactiveRulesForbiddenFields = new();\n\t\t\tactiveRuleSizes = new();\n\t\t\tstartForbiddenFields = Copy(forbidden);\n[conditionVariablesReset]\n";
            string conditionVariablesDeclare = "";
            string conditionVariablesReset = "";
            string conditionVariablesSet = "T(";
            activeRules = new() { "(no rule)" };

            listOfSizes = listOfSizes
            .OrderBy(x =>
            {
                int number;
                if (int.TryParse(new string(x.Where(char.IsDigit).ToArray()), out number))
                    return number;
                return -1;
            }).ToList().ToArray();

            foreach (string size in listOfSizes)
            {
                string sizeFolder = size.Replace(baseDir, "");
                string sizeNumber = sizeFolder.Replace("rules\\", "");
                TextBlock t = new();
                t.Text = sizeNumber + " x " + sizeNumber;
                t.FontSize = 14;
                t.FontWeight = FontWeights.Bold;
                t.Margin = new Thickness(10, 10 + childIndex / 2 * 35 + yTotalPos, 10, 10);
                t.HorizontalAlignment = HorizontalAlignment.Left;
                t.VerticalAlignment = VerticalAlignment.Top;
                MainGrid.Children.Insert(childIndex, t);
                Grid.SetRow(t, 1);

                Grid g = new();
                g.Margin = new Thickness(10, 35 + childIndex / 2 * 35 + yTotalPos, 10, 10);
                g.HorizontalAlignment = HorizontalAlignment.Left;
                g.VerticalAlignment = VerticalAlignment.Top;
                MainGrid.Children.Insert(++childIndex, g);
                Grid.SetRow(g, 1);

                if (int.Parse(sizeNumber) < 9)
                {
                    codeString += "\t\t\tif (size == " + sizeNumber + ")\n\t\t\t{\n";
                }
                else
                {
                    codeString += "\t\t\tif (size >= " + sizeNumber + ")\n\t\t\t{\n";
                }

                string[] listOfRules = Directory.GetFiles(baseDir + sizeFolder);

                int yPos = 0;
                foreach (string rule in listOfRules)
                {
                    string ruleName = System.IO.Path.GetFileName(rule);
                    if (ruleName.Substring(ruleName.Length - 3) == ".cs") continue;
                    string content = File.ReadAllText(baseDir + sizeFolder + "\\" + ruleName);
                    int pos = content.IndexOf("viewBox=\"0 0 ");
                    int lastPos = content.IndexOf("\"", pos + 13);
                    string sizeStr = content.Substring(pos + 13, lastPos - pos - 13);
                    string[] sizes = sizeStr.Split(" ");

                    ruleName = ruleName.Replace(".svg","");
                    string variableName = ruleName.Replace(" ", "").Replace("-", "");
                    if (variableName != "CShape")
                    {
                        conditionVariablesDeclare += "\t\tpublic bool " + variableName + " = false;\n";
                        conditionVariablesReset += "\t\t\t" + variableName + " = false;\n";
                        conditionVariablesSet += "\"" + variableName + ": \" + " + variableName + " + \"\\n\" + ";
                    }
                    codeString += "\t\t\t\t// " + ruleName + "\n";
                    activeRules.Add(ruleName);

                    pos = 0;
                    int section1Pos = content.IndexOf("<!--1-->");
                    int section2Pos = content.IndexOf("<!--2-->");
                    List<int[]> takenFields = new();
                    List<int[]> forbiddenFields = new();
                    int[]? noCornerField = null;
                    List<int[]> countAreaStartFields = new();
                    List<int[]> countAreaEndFields = new();
                    List<int[]> countAreaBorderFields = new();

                    while (true)
                    {
                        pos = content.IndexOf("<!-- ", pos);
                        if (pos == -1) break;
                        lastPos = content.IndexOf(" -->", pos);
                        string codeLine = content.Substring(pos + 5, lastPos - pos - 5);
                        string[] arr = codeLine.Split(" ");
                        int fieldX = int.Parse(arr[0]);
                        int fieldY = int.Parse(arr[1]);
                        int fieldCode = int.Parse(arr[2]);
                        if (pos < section1Pos)
                        {
                            takenFields.Add(new int[] { fieldX, fieldY, fieldCode });
                        }
                        else if (pos < section2Pos)
                        {
                            if (fieldCode == 13 || fieldCode == 16 || fieldCode == 19)
                            {
                                countAreaStartFields.Add(new int[] { fieldX, fieldY, fieldCode });
                            }
                            else if (fieldCode == 14 || fieldCode == 17 || fieldCode == 20)
                            {
                                countAreaEndFields.Add(new int[] { fieldX, fieldY, fieldCode });
                            }
                            else if (fieldCode == 15 || fieldCode == 18 || fieldCode == 21 || fieldCode == 22)
                            {
                                countAreaBorderFields.Add(new int[] { fieldX, fieldY, fieldCode });
                            }
                        }
                        else
                        {
                            if (fieldCode == 12)
                            {
                                forbiddenFields.Add(new int[] { fieldX, fieldY });
                            }
                            else
                            {
                                noCornerField = new int[] { fieldX, fieldY };
                            }
                        }
                        pos = lastPos;
                    }

                    t = new();
                    t.Text = ruleName;
                    t.Margin = new Thickness(0, yPos, 0, 0);
                    t.HorizontalAlignment = HorizontalAlignment.Left;
                    t.VerticalAlignment = VerticalAlignment.Top;

                    pos = content.IndexOf("<svg");
                    string[] metaArr = new string[] { "", "", "" };
                    if (pos != 0)
                    {
                        string meta = content.Substring(0, pos).Trim();
                        meta = meta.Replace("<!--|", "").Replace("|-->", "");
                        metaArr = meta.Split("|");

                        if (metaArr[0] == "True")
                        {
                            t.Text += " | Rotate clockwise";
                        }
                        if (metaArr[1] == "True")
                        {
                            t.Text += " | Rotate counter-clockwise";
                        }
                    }

                    g.Children.Add(t);

                    yPos += 21;

                    SKElement c = new();
                    c.IgnorePixelScaling = true;
                    c.Tag = sizeFolder + "\\" + ruleName.Replace(".svg", "");
                    c.PaintSurface += SKElement_PaintSurface;
                    c.Width = int.Parse(sizes[0]) * 40;
                    c.Height = int.Parse(sizes[1]) * 40;
                    c.Margin = new Thickness(0, yPos, 0, 0);
                    c.HorizontalAlignment = HorizontalAlignment.Left;
                    c.VerticalAlignment = VerticalAlignment.Top;
                    g.Children.Add(c);

                    yPos += int.Parse(sizes[1]) * 40 + 10;

                    
                    codeString += GenerateCode(variableName, metaArr, takenFields, forbiddenFields, noCornerField, countAreaStartFields, countAreaEndFields, countAreaBorderFields, ruleName, int.Parse(sizes[0]), int.Parse(sizes[1]));
                }
                codeString = codeString.Substring(0, codeString.Length - 1);
                codeString += "\t\t\t}\n\n";
                yTotalPos += yPos - 10;
                childIndex++;
            }
            RotateClockwise.IsChecked = false;
            RotateCounterClockwise.IsChecked = false;
            codeString = codeString.Substring(0, codeString.Length - 1);

            if (conditionVariablesSet.Length != 2)
            {
                codeString += "\t\t\t" + conditionVariablesSet.Substring(0, conditionVariablesSet.Length - 10) + ");\n\t\t\twindow.ShowActiveRules(activeRules,activeRulesForbiddenFields,startForbiddenFields,activeRuleSizes);\n\t\t}\n\t}\n}";
            }
            else
            {
                codeString += "\t\t}\n\t}\n}";
            }
            
            codeString = codeString.Replace("[conditionVariablesDeclare]", conditionVariablesDeclare);
            codeString = codeString.Replace("[conditionVariablesReset]", conditionVariablesReset);
            File.WriteAllText(baseDir + "PathRules.cs", codeString);
        }

        private string GenerateCode(string variableName, string[] meta, List<int[]> takenFields, List<int[]> forbiddenFields, int[]? noCornerField, List<int[]> countAreaStartFields, List<int[]> countAreaEndFields, List<int[]> countAreaBorderFields, string ruleName, int xSize, int ySize)
        {
            if (variableName == "CShape") return "\t\t\t\t// Embedded in Path.cs as the absolute checking functions need it.\n\n";

            int startX = 0;
            int startY = 0;

            foreach (int[] field in takenFields)
            {
                if (field[2] == 1)
                {
                    startX = field[0];
                    startY = field[1];
                }
            }

            string conditionStr = "";

            foreach (int[] field in takenFields)
            {
                int relX = startX - field[0];
                int relY = startY - field[1];
                switch (field[2])
                {
                    case 2:
                        conditionStr += "!InTakenRel(" + relX + "," + relY + ") && !InBorderRel(" + relX + "," + relY + ") && ";
                        break;
                    case 3:
                        conditionStr += "InTakenRel(" + relX + "," + relY + ") && ";
                        break;
                    case 4:
                    case 5:
                    case 6:
                    case 7:                        
                    case 8:
                        conditionStr += "(InTakenRel(" + relX + "," + relY + ") || InBorderRel(" + relX + "," + relY + ")) && ";
                        break;
                    case 9:
                        conditionStr += "InFutureStartRel(" + relX + "," + relY + ") && ";
                        break;
                    case 10:
                        conditionStr += "InFutureEndRel(" + relX + "," + relY + ") && ";
                        break;
                }
            }

            if (!(noCornerField is null))
            {
                int relX = startX - noCornerField[0];
                int relY = startY - noCornerField[1];
                conditionStr += "!InCornerRel(" + relX + "," + relY + ") && ";
            }

            conditionStr = conditionStr.Substring(0, conditionStr.Length - 4);

            // only works if only one future start and end fields are added
            if (conditionStr.IndexOf("InFutureStart") != -1 && conditionStr.IndexOf("InFutureEnd") != -1)
            {
                conditionStr += " && foundSectionStart == foundSectionEnd";
            }

            string forbiddenStr = "";
            string listOfForbiddenFields = "";

            foreach (int[] field in forbiddenFields)
            {
                int relX = startX - field[0];
                int relY = startY - field[1];
                if (relX == 0)
                {
                    forbiddenStr += "forbidden.Add(new int[] { x + sx, y + sy });\n";
                    listOfForbiddenFields += "new int[] { x + sx, y + sy }, ";
                }
                else if (relX == 1)
                {
                    forbiddenStr += "forbidden.Add(new int[] { x + lx, y + ly });\n";
                    listOfForbiddenFields += "new int[] { x + lx, y + ly }, ";
                }
                else
                {
                    forbiddenStr += "forbidden.Add(new int[] { x - lx, y - ly });\n";
                    listOfForbiddenFields += "new int[] { x - lx, y - ly }, ";
                }
            }
            forbiddenStr = forbiddenStr.Substring(0, forbiddenStr.Length - 1);

            string codeStr, ruleCore = "";

            if (countAreaStartFields.Count != 0)
            {
                forbiddenStr = "\t" + forbiddenStr.Replace("\n", "\n\t") + "\n";
                int circleCount = 0;
                string areaConditionsCode = "";
                string countAreaCodeStart = "ResetExamAreas();\nif (";
                string countAreaCodeEnd = "";

                foreach (int[] coord in countAreaStartFields)
                {
                    int sx = coord[0];
                    int sy = coord[1];
                    int stype = coord[2];
                    int foundEntry = 0;

                    foreach (int[] coord1 in countAreaEndFields)
                    {
                        int ex = coord1[0];
                        int ey = coord1[1];
                        int etype = coord1[2];

                        if (etype == stype + 1)
                        {
                            bool correctEnd = false;

                            circleCount++;
                            string countAreaCodeSection = "";

                            bool noCount = false;
                            int distX = 0, distY = 0;

                            if (Math.Abs(ex - sx) == 1 && ey == sy || Math.Abs(ey - sy) == 1 && ex == sx)
                            {                                
                                // if the area is next to the live end, count it. Otherwise, only check direction of the taken field, counting area is not necessary, and often it would only be 2.
                                correctEnd = true;
                               
                                distX = sx - ex;
                                distY = sy - ey;

                                if (Math.Abs(startX - sx) <= 1 && Math.Abs(startY - sy) <= 1)
                                {
                                    countAreaCodeSection += "CountAreaRel(" + (startX - sx) + "," + (startY - sy) + "," + (startX - ex) + "," + (startY - ey) + ",null,";
                                }
                                else
                                {
                                    noCount = true;
                                }
                            }
                            else
                            {
                                countAreaCodeSection += "CountAreaRel(" + (startX - sx) + "," + (startY - sy) + "," + (startX - ex) + "," + (startY - ey) + ",new List<int[]> {";

                                int dist = Math.Abs(sx - ex) + Math.Abs(sy - ey);
                                correctEnd = true;
                                for (int i = dist - 1; i >= 1; i--)
                                {
                                    bool foundDist = false; // there can be 2  count areas of the same type. If not all border fields are found, we are at the wrong end.

                                    foreach (int[] coord2 in countAreaBorderFields)
                                    {
                                        int bx = coord2[0];
                                        int by = coord2[1];
                                        int btype = coord2[2];

                                        if ((btype == stype + 2 || btype == stype + 3) && (Math.Abs(sx - bx) + Math.Abs(sy - by) == i))
                                        {
                                            foundDist = true;
                                            countAreaCodeSection += "new int[] {" + (startX - bx) + "," + (startY - by) + "},";

                                            if (i == dist - 1)
                                            {
                                                distX = bx - ex;
                                                distY = by - ey;
                                            }

                                            if (btype == stype + 3)
                                            {
                                                foundEntry = 1;
                                            }

                                            break;
                                        }
                                    }

                                    if (!foundDist)
                                    {
                                        correctEnd = false;
                                        break;
                                    }
                                }

                                if (!correctEnd)
                                {
                                    circleCount--;
                                    continue;
                                }

                                countAreaCodeSection = countAreaCodeSection.Substring(0, countAreaCodeSection.Length - 1) + "},";
                            }

                            bool directionLeft;

                            directionField = null;

                            directionLeft = GetCircleDirection(ex, ey, distX, distY, takenFields);

                            if (directionField == null) // end was not correct, because two areas are next to each other
                            {
                                circleCount--;
                                continue;
                            }

                            countAreaCodeStart += variableName + "_circle" + circleCount + " && ";
                            if (!noCount)
                            {
                                countAreaCodeEnd += countAreaCodeSection + "i==0?" + directionLeft.ToString().ToLower() + ":!" + directionLeft.ToString().ToLower() + "," + ((stype - stype % 3) / 3 - 4 + foundEntry) + ") && ";
                            }

                            // countAreaRule.Replace("\t\t", "\t\t\t") +

                            int directionX = startX - directionField[0];
                            int directionY = startY - directionField[1];
                            int leftX = startX - (directionField[0] + directions[leftDir][0]);
                            int leftY = startY - (directionField[1] + directions[leftDir][1]);
                            int rightX = startX - (directionField[0] + directions[rightDir][0]);
                            int rightY = startY - (directionField[1] + directions[rightDir][1]);

                            areaConditionsCode += "bool " + variableName + "_circle" + circleCount + " = false;\n" +
                    "directionFieldIndex = InTakenIndexRel(" + directionX + "," + directionY + ");\n" +
                    "if (directionFieldIndex != -1)\n" +
                    "{\n" +
                    "\tif (InTakenRel(" + leftX + "," + leftY + "))\n" +
                    "\t{\n" +
                    "\t\tint leftIndex = InTakenIndexRel(" + leftX + "," + leftY + ");\n" +
                    "\t\tif (leftIndex " + (directionLeft ? ">" : "<") + " directionFieldIndex)\n" +
                    "\t\t{\n" +
                    "\t\t\t" + variableName + "_circle" + circleCount + " = true;\n" +
                    "\t\t}\n" +
                    "\t}\n" +
                    "\telse\n" +
                    "\t{\n" +
                    "\t\tint rightIndex = InTakenIndexRel(" + rightX + "," + rightY + ");\n" +
                    "\t\tif (rightIndex " + (directionLeft ? "<" : ">") + " directionFieldIndex)\n" +
                    "\t\t{\n" +
                    "\t\t\t" + variableName + "_circle" + circleCount + " = true;\n" +
                    "\t\t}\n" +
                    "\t}\n" +
                    "}\n" +
                    "else\n" +
                    "{\n" +
                    "\tdirectionFieldIndex = InBorderIndexRel(" + directionX + "," + directionY + ");\n" +
                    "\tint farSideIndex = InBorderIndexRel(" + (directionLeft ? rightX : leftX) + "," + (directionLeft ? rightY : leftY) + ");\n" +
                    "\tif (farSideIndex > directionFieldIndex)\n" +
                    "\t{\n" +
                    "\t\t" + variableName + "_circle" + circleCount + " = true;\n" +
                    "\t}\n" +
                    "}\n\n";

                            break; // Found the matching end, break cycling through end coordinates
                        }
                    }
                }
                if (countAreaCodeEnd.Length > 0)
                {
                    countAreaCodeEnd = countAreaCodeEnd.Substring(0, countAreaCodeEnd.Length - 4) + ")\n";
                }
                else
                {
                    countAreaCodeStart = countAreaCodeStart.Substring(0, countAreaCodeStart.Length - 4) + ")\n";
                }              

                ruleCore = areaConditionsCode + countAreaCodeStart + countAreaCodeEnd +
                    "{\n" +
                    "\t" + variableName + " = true;\n" +
                    "\tactiveRules.Add(\"" + ruleName + "\");\n" +
                    "\tactiveRulesForbiddenFields.Add(new List<int[]> {" + listOfForbiddenFields.Substring(0, listOfForbiddenFields.Length-2) + "});\n" +
                    "\tactiveRuleSizes.Add(new int[] {" + xSize + "," + ySize + "});\n" +
                    "\tAddExamAreas();\n" +
                    forbiddenStr +
                    "}";
            }
            else
            {
                ruleCore = variableName + " = true;\n" +
                    "activeRules.Add(\"" + ruleName + "\");\n" +
                    "activeRulesForbiddenFields.Add(new List<int[]> {" + listOfForbiddenFields.Substring(0, listOfForbiddenFields.Length - 2) + "});\n" +
                    "activeRuleSizes.Add(new int[] {" + xSize + "," + ySize + "});\n" +                    
                    forbiddenStr;
            }

            //ruleCore = variableName + " = true;\n" + forbiddenStr;

            if (meta[0] == "True")
            {
                ruleCore = "\t\t\t" + ruleCore.Replace("\n", "\n\t\t\t") + "\n";
                codeStr = "for (int i = 0; i < 2; i++)\n" +
                    "{\n" +
                    "\tfor (int j = 0; j < 2; j++)\n" +
                    "\t{\n" +
                    "\t\tif (" + conditionStr + ")\n" +
                    "\t\t{\n" +
                    ruleCore +
                    "\t\t}\n" +
                    "\t\tint s0 = sx;\n" +
                    "\t\tint s1 = sy;\n" +
                    "\t\tsx = -lx;\n" +
                    "\t\tsy = -ly;\n" +
                    "\t\tlx = s0;\n" +
                    "\t\tly = s1;\n" +
                    "\t}\n" +
                    "\tsx = thisSx;\n" +
                    "\tsy = thisSy;\n" +
                    "\tlx = -thisLx;\n" +
                    "\tly = -thisLy;\n" +
                    "}\n" +
                    "sx = thisSx;\n" +
                    "sy = thisSy;\n" +
                    "lx = thisLx;\n" +
                    "ly = thisLy;";

            }
            else if (meta[1] == "True")
            {
                ruleCore = "\t\t\t" + ruleCore.Replace("\n", "\n\t\t\t") + "\n";
                codeStr = "for (int i = 0; i < 2; i++)\n" +
                    "{\n" +
                    "\tfor (int j = 0; j < 2; j++)\n" +
                    "\t{\n" +
                    "\t\tif (" + conditionStr + ")\n" +
                    "\t\t{\n" +
                    ruleCore +
                    "\t\t}\n" +
                    "\t\tint l0 = lx;\n" +
                    "\t\tint l1 = ly;\n" +
                    "\t\tlx = -sx;\n" +
                    "\t\tly = -sy;\n" +
                    "\t\tsx = l0;\n" +
                    "\t\tsy = l1;\n" +
                    "\t}\n" +
                    "\tsx = thisSx;\n" +
                    "\tsy = thisSy;\n" +
                    "\tlx = -thisLx;\n" +
                    "\tly = -thisLy;\n" +
                    "}\n" +
                    "sx = thisSx;\n" +
                    "sy = thisSy;\n" +
                    "lx = thisLx;\n" +
                    "ly = thisLy;";
            }
            else
            {
                ruleCore = "\t\t" + ruleCore.Replace("\n", "\n\t\t") + "\n";
                codeStr = "for (int i = 0; i < 2; i++)\n" +
                    "{\n" +
                    "\tif (" + conditionStr + ")\n" +
                    "\t{\n" +
                    ruleCore +
                    "\t}\n" +
                    "\tlx = -lx;\n" +
                    "\tly = -ly;\n" +
                    "}\n" +
                    "lx = thisLx;\n" +
                    "ly = thisLy;";
            }

            codeStr = "\t\t\t\t" + codeStr.Replace("\n", "\n\t\t\t\t") + "\n\n";

            return codeStr;
        }*/


        // ----- Metadata settings -----

        
        /*private void RotateClockwise_Click(object sender, RoutedEventArgs e)
        {
            if (RotateClockwise.IsChecked == null || (bool)RotateClockwise.IsChecked)
            {
                RotateCounterClockwise.IsChecked = false;
            }
            SaveMeta();
        }

        private void RotateCounterClockwise_Click(object sender, RoutedEventArgs e)
        {
            if (RotateCounterClockwise.IsChecked == null || (bool)RotateCounterClockwise.IsChecked)
            {
                RotateClockwise.IsChecked = false;
            }
            SaveMeta();
        }*/

        /*private void CircleDirectionLeft_Click(object sender, RoutedEventArgs e)
        {*/
            /*if (!(arrowStart is null) && !(arrowEnd is null))
            {
                int pos = newRule.IndexOf("<!-- " + arrowStart[0] + " " + arrowStart[1] + " " + arrowEnd[0] + " " + arrowEnd[1] + " 15 -->");

                newRule = newRule.Substring(0, pos) + "<!-- " + arrowEnd[0] + " " + arrowEnd[1] + " " + arrowStart[0] + " " + arrowStart[1] + " 15 -->\n\t<path d=\"" + DrawArrow(arrowEnd[0], arrowEnd[1], arrowStart[0], arrowStart[1]) + "\" fill=\"white\" fill-opacity=\"0\" stroke=\"black\" stroke-width=\"0.05\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />\n</svg>";
            }*/

            /*File.WriteAllText(baseDir + svgName, newRule);
            Canvas.InvalidateVisual();
            SaveMeta();
        }*/

        /*private void SaveMeta()
        {
            if (newRule == null) return;
            int pos = newRule.IndexOf("<svg");
            newRule = "<!--|" + RotateClockwise.IsChecked + "|" + RotateCounterClockwise.IsChecked + "|-->\n" + newRule.Substring(pos);
            File.WriteAllText(baseDir + svgName, newRule);
        }*/


        // ----- Log and helper functions -----


        private void T(object o)
        {
            Trace.WriteLine(o.ToString());
            MainWindow.logger.LogDebug("----------------------------- " + o);
        }

        private void M(object o)
        {
            MessageBox.Show(o.ToString());
        }

        private int FindDirection(int xDiff, int yDiff)
        {
            for (int i = 0; i < 4; i++)
            {
                if (directions[i][0] == xDiff && directions[i][1] == yDiff)
                {
                    return i;
                }
            }
            return 0;
        }

        private void SKElement_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            string fileName = (string)((SKElement)sender).Tag + ".svg";
            if (!File.Exists(baseDir + fileName)) return;
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            var svg = new SkiaSharp.Extended.Svg.SKSvg();
            var picture = svg.Load(baseDir + fileName);

            var fit = e.Info.Rect.AspectFit(svg.CanvasSize.ToSizeI());
            e.Surface.Canvas.Scale(fit.Width / svg.CanvasSize.Width);
            e.Surface.Canvas.DrawPicture(picture);
        }
    }
}
