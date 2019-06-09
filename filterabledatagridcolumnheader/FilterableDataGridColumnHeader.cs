using com.rocco.vincenzo.csharp.controls.datamodels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace com.rocco.vincenzo.csharp.controls.primitives
{
    /// <summary>  
    ///  This class adds filtering capabilities to DataGridColumnHeader. T must extend BaseFilterableData
    /// </summary>
    public class FilterableDataGridColumnHeader<T> : DataGridColumnHeader where T : BaseFilterableData
    {
        // Global variables declarations

        DependencyObject RootObject;
        Border HeaderBorder;
        TextBox HeaderTextBox;
        Thickness StandardHeaderTextBoxBorderThickness = new Thickness(0.5, 1, 0, 1);
        ToggleButton HeaderToggle;
        Thickness StandardHeaderToggleBorderThickness = new Thickness(0, 1, 0.5, 1);
        SolidColorBrush StandardBorderColor = new SolidColorBrush(Colors.Black);
        Popup HeaderPopUp;
        BitmapImage ImagePopBitmap;
        Button OkButton;
        Image ImageArrowBlack;
        Image ImageArrowRed;
        List<String> FilteredOutValues;
        List<String> SearchedListResult;
        ObservableCollection<String> TemporaryFilteredOutValues;
        ObservableCollection<String> InSearchTemporaryFilteredOutValues;
        Border BorderForStackChecks;
        ScrollViewer ViewerForCheckBoxes;
        StackPanel StackForCheckBoxes;
        List<CheckBox> ItemsCheckBoxes;
        IList<T> TotalDataSource;
        ICollectionView DataCollectionView;
        String HeaderTextNoUnderscore;
        String OldHeaderTextNoUnderscore;
        List<String> PopUpFilterValues;
        static Dictionary<String, Predicate<T>> DictionaryFilter = new Dictionary<String, Predicate<T>>();


        // Bindable Dependency Properties section

        /// <summary>  
        ///  ReadOnly Property showing whether a filtered is applied in the column
        /// </summary>
        public bool IsFiltered
        {
            get { return (bool)this.GetValue(IsFilteredProperty); }
            private set { this.SetValue(IsFilteredProperty, value); }
        }

        public static readonly DependencyProperty IsFilteredProperty = DependencyProperty.Register(
          "IsFiltered", typeof(bool), typeof(FilterableDataGridColumnHeader<T>), new PropertyMetadata(false));

        /// <summary>  
        ///  ReadOnly Property returning the current width of the column
        /// </summary>
        public new double Width
        {
            get { return (double)this.GetValue(WidthProperty); }
        }

        public new static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
          "Width", typeof(double), typeof(FilterableDataGridColumnHeader<T>));

        /// <summary>  
        ///  ReadOnly Property returning the max width of the column
        /// </summary>
        public new double MaxWidth
        {
            get { return (double)this.GetValue(MaxWidthProperty); }
        }

        public new static readonly DependencyProperty MaxWidthProperty = DependencyProperty.Register(
          "MaxWidth", typeof(double), typeof(FilterableDataGridColumnHeader<T>));

        /// <summary>  
        ///  ReadOnly Property returning the min width of the column
        /// </summary>
        public new double MinWidth
        {
            get { return (double)this.GetValue(MinWidthProperty); }
        }

        public new static readonly DependencyProperty MinWidthProperty = DependencyProperty.Register(
          "MinWidth", typeof(double), typeof(FilterableDataGridColumnHeader<T>));

        /// <summary>  
        ///  ReadOnly Property returning the current height of the column
        /// </summary>
        public new double Height
        {
            get { return (double)this.GetValue(HeightProperty); }
        }

        public new static readonly DependencyProperty HeightProperty = DependencyProperty.Register(
          "Height", typeof(double), typeof(FilterableDataGridColumnHeader<T>));

        /// <summary>  
        ///  ReadOnly Property returning the max height of the column
        /// </summary>
        public new double MaxHeight
        {
            get { return (double)this.GetValue(MaxHeightProperty); }
        }

        public new static readonly DependencyProperty MaxHeightProperty = DependencyProperty.Register(
          "MaxHeight", typeof(double), typeof(FilterableDataGridColumnHeader<T>));

        /// <summary>  
        ///  ReadOnly Property returning the min height of the column
        /// </summary>
        public new double MinHeight
        {
            get { return (double)this.GetValue(MinHeightProperty); }
        }

        public new static readonly DependencyProperty MinHeightProperty = DependencyProperty.Register(
          "MinHeight", typeof(double), typeof(FilterableDataGridColumnHeader<T>));

        /// <summary>  
        ///  Property for the current tickness of the border around the header
        /// </summary>
        public new Thickness BorderThickness
        {
            get { return (Thickness)this.GetValue(BorderThicknessProperty); }
            set
            {
                this.SetValue(BorderThicknessProperty, value);
                HeaderBorder.BorderThickness = value;
            }
        }

        public new static readonly DependencyProperty BorderThicknessProperty = DependencyProperty.Register(
          "BorderThickness", typeof(Thickness), typeof(FilterableDataGridColumnHeader<T>));

        /// <summary>  
        ///  Property for the current color of the border around the header
        /// </summary>
        public new Brush BorderBrush
        {
            get { return (Brush)this.GetValue(BorderBrushProperty); }
            set
            {
                this.SetValue(BorderBrushProperty, value);
                HeaderBorder.BorderBrush = value;
            }
        }

        public new static readonly DependencyProperty BorderBrushProperty = DependencyProperty.Register(
          "BorderBrush", typeof(Brush), typeof(FilterableDataGridColumnHeader<T>));

        /// <summary>  
        ///  Property for the current text used inside the header: the setter can throw InvalidOperationException in case the name used is already present in the underlying model Class
        /// </summary>
        public String HeaderText
        {
            get { return (String)this.GetValue(HeaderTextProperty); }
            set
            {
                this.SetValue(HeaderTextProperty, value);
                if (HeaderTextBox != null)
                {
                    OldHeaderTextNoUnderscore = HeaderTextNoUnderscore;
                    HeaderTextBox.Text = value;
                    HeaderTextNoUnderscore = value.Replace("_", "");
                    for (int i = DictionaryFilter.Count - 1; i >= 0; i--)
                    {
                        if (DictionaryFilter.ElementAt(i).Key.Contains(OldHeaderTextNoUnderscore))
                        {
                            var currentValue = DictionaryFilter.ElementAt(i).Value;
                            var newKey = DictionaryFilter.ElementAt(i).Key.Replace(OldHeaderTextNoUnderscore, HeaderTextNoUnderscore);
                            DictionaryFilter.Remove(DictionaryFilter.ElementAt(i).Key);
                            DictionaryFilter.Add(newKey, currentValue);
                        }
                    }
                    for (int i = TotalDataSource.Count() - 1; i >= 0; i--)
                    {
                        var basedata = TotalDataSource.ElementAt(i) as BaseFilterableData;
                        basedata.ModifyHeaderName(OldHeaderTextNoUnderscore, HeaderTextNoUnderscore);
                    }
                }
            }
        }

        public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(
          "HeaderText", typeof(String), typeof(FilterableDataGridColumnHeader<T>), new PropertyMetadata(String.Empty));

        /// <summary>  
        ///  Property for the current size of the text used inside the header
        /// </summary>
        public double HeaderTextSize
        {
            get { return (double)this.GetValue(HeaderTextSizeProperty); }
            set
            {
                this.SetValue(HeaderTextSizeProperty, value);
                if (HeaderTextBox != null)
                {
                    HeaderTextBox.FontSize = value;
                }
            }
        }

        public static readonly DependencyProperty HeaderTextSizeProperty = DependencyProperty.Register(
            "HeaderTextSize", typeof(double), typeof(FilterableDataGridColumnHeader<T>), new PropertyMetadata(17.0));

        /// <summary>  
        ///  Property for the current stretch type of the text used inside the header
        /// </summary>
        public FontStretch HeaderTextStretch
        {
            get { return (FontStretch)this.GetValue(HeaderTextStretchProperty); }
            set
            {
                this.SetValue(HeaderTextStretchProperty, value);
                if (HeaderTextBox != null)
                {
                    HeaderTextBox.FontStretch = value;
                }
            }
        }

        public static readonly DependencyProperty HeaderTextStretchProperty = DependencyProperty.Register(
            "HeaderTextStretch", typeof(FontStretch), typeof(FilterableDataGridColumnHeader<T>), new PropertyMetadata(FontStretches.Normal));

        /// <summary>  
        ///  Property for the current font of the text used inside the header
        /// </summary>
        public FontFamily HeaderTextFamily
        {
            get { return (FontFamily)this.GetValue(HeaderTextFamilyProperty); }
            set
            {
                this.SetValue(HeaderTextFamilyProperty, value);
                if (HeaderTextBox != null)
                {
                    HeaderTextBox.FontFamily = value;
                }
            }
        }

        public static readonly DependencyProperty HeaderTextFamilyProperty = DependencyProperty.Register(
            "HeaderTextFamily", typeof(FontFamily), typeof(FilterableDataGridColumnHeader<T>), new PropertyMetadata(new FontFamily("Verdana")));

        /// <summary>  
        ///  Property for the current style of the text used inside the header
        /// </summary>
        public FontStyle HeaderTextStyle
        {
            get { return (FontStyle)this.GetValue(HeaderTextStyleProperty); }
            set
            {
                this.SetValue(HeaderTextStyleProperty, value);
                if (HeaderTextBox != null)
                {
                    HeaderTextBox.FontStyle = value;
                }
            }
        }

        public static readonly DependencyProperty HeaderTextStyleProperty = DependencyProperty.Register(
            "HeaderTextStyle", typeof(FontStyle), typeof(FilterableDataGridColumnHeader<T>), new PropertyMetadata(FontStyles.Normal));

        /// <summary>  
        ///  Property for the current weight of the text used inside the header
        /// </summary>
        public FontWeight HeaderTextWeight
        {
            get { return (FontWeight)this.GetValue(HeaderTextWeightProperty); }
            set
            {
                this.SetValue(HeaderTextWeightProperty, value);
                if (HeaderTextBox != null)
                {
                    HeaderTextBox.FontWeight = value;
                }
            }
        }

        public static readonly DependencyProperty HeaderTextWeightProperty = DependencyProperty.Register(
            "HeaderTextWeight", typeof(FontWeight), typeof(FilterableDataGridColumnHeader<T>), new PropertyMetadata(FontWeights.Normal));

        /// <summary>
        ///  Property for the current decoration of the text used inside the header
        /// </summary>
        public TextDecorationCollection HeaderTextDecoration
        {
            get { return (TextDecorationCollection)this.GetValue(HeaderTextDecorationProperty); }
            set
            {
                this.SetValue(HeaderTextDecorationProperty, value);
                if (HeaderTextBox != null)
                {
                    HeaderTextBox.TextDecorations = value;
                }
            }
        }

        public static readonly DependencyProperty HeaderTextDecorationProperty = DependencyProperty.Register(
            "HeaderTextDecoration", typeof(TextDecorationCollection), typeof(FilterableDataGridColumnHeader<T>));

        /// <summary>
        ///  Property for whether the text used inside the header will be wrapped to fit reduced width, causing height increase
        /// </summary>
        public bool WrapText
        {
            get { return (bool)this.GetValue(WrapTextProperty); }
            set
            {
                this.SetValue(WrapTextProperty, value);
                if (value == true)
                {
                    HeaderTextBox.TextWrapping = TextWrapping.Wrap;
                }
                else
                {
                    HeaderTextBox.TextWrapping = TextWrapping.NoWrap;
                }
            }
        }

        public static readonly DependencyProperty WrapTextProperty = DependencyProperty.Register(
          "WrapText", typeof(bool), typeof(FilterableDataGridColumnHeader<T>), new PropertyMetadata(false));

        /// <summary>
        ///  Property for how the text used inside the header will be horizontally aligned
        /// </summary>
        public HorizontalAlignment HeaderTextHorizontalAlignment
        {
            get { return (HorizontalAlignment)this.GetValue(HeaderTextHorizontalAlignmentProperty); }
            set
            {
                this.SetValue(HeaderTextHorizontalAlignmentProperty, value);
                if (HeaderTextBox != null)
                {
                    HeaderTextBox.HorizontalContentAlignment = value;
                }
            }
        }

        public static readonly DependencyProperty HeaderTextHorizontalAlignmentProperty = DependencyProperty.Register(
            "HeaderTextHorizontalAlignment", typeof(HorizontalAlignment), typeof(FilterableDataGridColumnHeader<T>));

        /// <summary>
        ///  Property for how the text used inside the header will be vertically aligned
        /// </summary>
        public VerticalAlignment HeaderTextVerticalAlignment
        {
            get { return (VerticalAlignment)this.GetValue(HeaderTextVerticalAlignmentProperty); }
            set
            {
                this.SetValue(HeaderTextVerticalAlignmentProperty, value);
                if (HeaderTextBox != null)
                {
                    HeaderTextBox.VerticalContentAlignment = value;
                }
            }
        }

        public static readonly DependencyProperty HeaderTextVerticalAlignmentProperty = DependencyProperty.Register(
            "HeaderTextVerticalAlignment", typeof(VerticalAlignment), typeof(FilterableDataGridColumnHeader<T>));

        /// <summary>
        ///  Property to enable filtering of the particuar column
        /// </summary>
        public bool Filter
        {
            get { return (bool)this.GetValue(FilterProperty); }
            set
            {
                this.SetValue(FilterProperty, value);
                if (value == true)
                {
                    HeaderToggle.Visibility = Visibility.Visible;
                }
                else
                {
                    HeaderToggle.Visibility = Visibility.Collapsed;
                    RemoveDictionaryFilter();
                }
            }
        }

        public static readonly DependencyProperty FilterProperty = DependencyProperty.Register(
          "Filter", typeof(bool), typeof(FilterableDataGridColumnHeader<T>));

        /// <summary>
        ///  Class constructor: The first parameter is the text which will be displayed in the header, the second is the data source of the grid, the third the collection view obtained from the source
        /// </summary>
        public FilterableDataGridColumnHeader(String text, IList<T> totalSource, ICollectionView collectionView)
        {
            // Global variable initialization

            FilteredOutValues = new List<String>();
            SearchedListResult = new List<String>();
            TemporaryFilteredOutValues = new ObservableCollection<String>();
            InSearchTemporaryFilteredOutValues = new ObservableCollection<String>();
            StackForCheckBoxes = new StackPanel();
            StackForCheckBoxes.Orientation = Orientation.Vertical;
            ItemsCheckBoxes = new List<CheckBox>();
            TotalDataSource = totalSource;
            DataCollectionView = collectionView;
            this.Background = new SolidColorBrush(Colors.LightGray);
            HeaderText = text;
            HeaderTextNoUnderscore = HeaderText.Replace("_", "");


            // Loading of layout and pictures from package resources to create gui and icon images

            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream BlackArrowImageStream = assembly.GetManifestResourceStream("filterabledatagridcolumnheader.Resources.Pictures.BlackArrowDown.png"),
                RedArrowImageStream = assembly.GetManifestResourceStream("filterabledatagridcolumnheader.Resources.Pictures.RedArrowDown.png"),
                PopArrowImageStream = assembly.GetManifestResourceStream("filterabledatagridcolumnheader.Resources.Pictures.ResizePopArrow.png"),
                LayoutStream = assembly.GetManifestResourceStream("filterabledatagridcolumnheader.Resources.Layouts.FilterableDataGridColumnHeaderGUI.xaml"))
            {
            BitmapImage ImageBlackArrowBitmap = new BitmapImage();
                ImageBlackArrowBitmap.BeginInit();
                ImageBlackArrowBitmap.StreamSource = BlackArrowImageStream;
                ImageBlackArrowBitmap.EndInit();
                ImageArrowBlack = new Image();
                ImageArrowBlack.Source = ImageBlackArrowBitmap;
                BitmapImage ImageRedArrowBitmap = new BitmapImage();
                ImageRedArrowBitmap.BeginInit();
                ImageRedArrowBitmap.StreamSource = RedArrowImageStream;
                ImageRedArrowBitmap.EndInit();
                ImageArrowRed = new Image();
                ImageArrowRed.Source = ImageRedArrowBitmap;
                ImagePopBitmap = new BitmapImage();
                ImagePopBitmap.BeginInit();
                ImagePopBitmap.StreamSource = PopArrowImageStream;
                ImagePopBitmap.EndInit();
                StreamReader LayoutStreamReader = new StreamReader(LayoutStream);
                RootObject = XamlReader.Load(LayoutStreamReader.BaseStream) as DependencyObject;
            }


            // Control layout definition (Grid of 1 row and 2 columns containig a TextBox and a ToggleButton)

            HeaderBorder = LogicalTreeHelper.FindLogicalNode(RootObject, "ControlBorder") as Border;
            HeaderTextBox = LogicalTreeHelper.FindLogicalNode(RootObject, "ControlTextBox") as TextBox;
            HeaderTextBox.Text = HeaderText;
            var formattedText = new FormattedText(HeaderText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(HeaderTextFamily, HeaderTextStyle, HeaderTextWeight, HeaderTextStretch), HeaderTextSize, Brushes.Black, new NumberSubstitution(), TextFormattingMode.Display);
            if (formattedText.Width < 75)
            {
                HeaderTextBox.MinWidth = 75;
            }
            HeaderToggle = LogicalTreeHelper.FindLogicalNode(RootObject, "ControlToggle") as ToggleButton;
            HeaderToggle.Content = ImageArrowBlack;
            HeaderToggle.Visibility = Visibility.Collapsed;
            HeaderToggle.Checked += delegate (object sender, RoutedEventArgs rea)
            {
                CreatePopUpOnToggled();
                HeaderPopUp.IsOpen = true;
            };
            this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            this.VerticalContentAlignment = VerticalAlignment.Stretch;
            this.Content = RootObject;
        }


        // Event handlers sections

        void ActOnCheckEvent(object sender, RoutedEventArgs r)
        {
            CheckBox cb = sender as CheckBox;
            if (TemporaryFilteredOutValues.Contains((String)cb.Content))
            {
                TemporaryFilteredOutValues.Remove((String)cb.Content);
            }
        }

        void ActOnUnCheckEvent(object sender, RoutedEventArgs r)
        {
            CheckBox cb = sender as CheckBox;
            if (!TemporaryFilteredOutValues.Contains((String)cb.Content))
            {
                TemporaryFilteredOutValues.Add((String)cb.Content);
            }
        }

        void ActOnCheckEventInSearch(object sender, RoutedEventArgs r)
        {
            CheckBox cb = sender as CheckBox;
            if (InSearchTemporaryFilteredOutValues.Contains((String)cb.Content))
            {
                InSearchTemporaryFilteredOutValues.Remove((String)cb.Content);
            }
        }

        void ActOnUnCheckEventInSearch(object sender, RoutedEventArgs r)
        {
            CheckBox cb = sender as CheckBox;
            if (!InSearchTemporaryFilteredOutValues.Contains((String)cb.Content))
            {
                InSearchTemporaryFilteredOutValues.Add((String)cb.Content);
            }
        }

        void ActOnOkCancelPressed(object sender, RoutedEventArgs r)
        {
            Button btn = sender as Button;
            if (String.Equals((String)btn.Tag, "OK"))
            {
                if (SearchedListResult.Count > 0)
                {
                    FilteredOutValues.Clear();
                    if (InSearchTemporaryFilteredOutValues.Count == 0)
                    {
                        PopUpFilterValues.Except(SearchedListResult).ToList().ForEach(FilteredOutValues.Add);
                    }
                    else
                    {
                        PopUpFilterValues.Except(SearchedListResult.Except(InSearchTemporaryFilteredOutValues)).ToList().ForEach(FilteredOutValues.Add);
                    }
                }
                else
                {
                    String[] initFiltered = new String[TemporaryFilteredOutValues.Count];
                    TemporaryFilteredOutValues.CopyTo(initFiltered, 0);
                    FilteredOutValues = initFiltered.ToList();
                }
                FilterOnValue();
                DataCollectionView.Filter = FilterCandidates;
            }
            HeaderPopUp.IsOpen = false;
            if (FilteredOutValues.Count > 0)
            {
                IsFiltered = true;
                HeaderToggle.Content = ImageArrowRed;
            }
            else
            {
                IsFiltered = false;
                HeaderToggle.Content = ImageArrowBlack;
            }
        }

        void OnDragThumbStarted(object sender, DragStartedEventArgs e)
        {
            Thumb t = (Thumb)sender;
            t.Cursor = Cursors.Hand;
        }

        void OnDragThumbDelta(object sender, DragDeltaEventArgs e)
        {
            double verticalVariation = HeaderPopUp.Height + e.VerticalChange;
            double horizontalVariation = HeaderPopUp.Width + e.HorizontalChange;
            if ((horizontalVariation >= 0) && (verticalVariation >= 0))
            {
                HeaderPopUp.Width = horizontalVariation;
                HeaderPopUp.Height = verticalVariation;
            }
        }

        void OnDragThumbCompleted(object sender, DragCompletedEventArgs e)
        {
            Thumb t = (Thumb)sender;
            t.Cursor = null;
        }

        void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox t = sender as TextBox;
            SearchedListResult.Clear();
            TemporaryFilteredOutValues.Clear();
            InSearchTemporaryFilteredOutValues.Clear();

            ItemsCheckBoxes.Clear();

            if (t.Text.Length <= 0)
            {
                PopulateStackWithCheckBoxes(PopUpFilterValues, true);
            }
            else
            {
                SearchedListResult = PopUpFilterValues.Where(o => o.IndexOf(t.Text, StringComparison.InvariantCultureIgnoreCase) >= 0).Distinct().OrderBy(o => o).ToList();
                if (SearchedListResult.Count > 0)
                {
                    PopulateStackWithCheckBoxes(SearchedListResult, false);
                }
                else
                {
                    StackForCheckBoxes.Children.Clear();
                    Label noMatches = new Label();
                    noMatches.Content = "No Matches";
                    noMatches.Margin = new Thickness(0, 10, 0, 0);
                    noMatches.HorizontalAlignment = HorizontalAlignment.Center;
                    noMatches.VerticalAlignment = VerticalAlignment.Center;
                    StackForCheckBoxes.Children.Add(noMatches);
                    OkButton.IsEnabled = false;
                }
            }
        }

        void OnSelectAllCheckCLicked(CheckBox chbx)
        {
            BindingExpression selectallCheckBoxbindingExpression = chbx.GetBindingExpression(ToggleButton.IsCheckedProperty);
            Binding selectallCheckParentBinding = selectallCheckBoxbindingExpression.ParentBinding;
            if (chbx.IsChecked.HasValue)
            {
                ((ObservableCollection<String>)selectallCheckParentBinding.Source).Clear();
                foreach (CheckBox c in ItemsCheckBoxes)
                {
                    c.IsChecked = true;
                }
            }
            else
            {
                foreach (CheckBox c in ItemsCheckBoxes)
                {
                    c.IsChecked = false;
                    if (!((ObservableCollection<String>)selectallCheckParentBinding.Source).Contains((String)c.Content))
                    {
                        ((ObservableCollection<String>)selectallCheckParentBinding.Source).Add((String)c.Content);
                    }
                }
            }
        }


        // Helper method: "PopulateStackWithCheckBoxes", logic to opulate the StackPanel in the PopUp with the needed CheckBoxes
        // Helper method: "CreatePopUpOnToggled", logic to create the PopUp itself after the ToggleButton is clicked

        void PopulateStackWithCheckBoxes(List<String> l, bool checkOnFilteredValueNeeded)
        {
            StackForCheckBoxes.Children.Clear();
            ItemsCheckBoxes.Clear();
            bool isBlankFound = false;
            CheckBox selectallCheckBox = new CheckBox();
            selectallCheckBox.IsThreeState = true;
            selectallCheckBox.Margin = new Thickness(2);
            Binding SelectAllTemporaryFilteredOutValuesBinding = new Binding("Count");
            SelectAllTemporaryFilteredOutValuesBinding.Mode = BindingMode.OneWay;
            SelectAllTemporaryFilteredOutValuesBinding.Converter = new ConvertCountToChecked();
            SelectAllTemporaryFilteredOutValuesBinding.ConverterParameter = l.Count;
            Binding OKButtonTemporaryFilteredOutValuesBinding = new Binding("Count");
            OKButtonTemporaryFilteredOutValuesBinding.Mode = BindingMode.OneWay;
            OKButtonTemporaryFilteredOutValuesBinding.Converter = new ConvertCountToEnabled();
            OKButtonTemporaryFilteredOutValuesBinding.ConverterParameter = l.Count;
            if (checkOnFilteredValueNeeded)
            {
                SelectAllTemporaryFilteredOutValuesBinding.Source = TemporaryFilteredOutValues;
                OKButtonTemporaryFilteredOutValuesBinding.Source = TemporaryFilteredOutValues;
            }
            else
            {
                SelectAllTemporaryFilteredOutValuesBinding.Source = InSearchTemporaryFilteredOutValues;
                OKButtonTemporaryFilteredOutValuesBinding.Source = InSearchTemporaryFilteredOutValues;
            }
            selectallCheckBox.SetBinding(ToggleButton.IsCheckedProperty, SelectAllTemporaryFilteredOutValuesBinding);
            OkButton.SetBinding(Button.IsEnabledProperty, OKButtonTemporaryFilteredOutValuesBinding);
            selectallCheckBox.VerticalAlignment = VerticalAlignment.Center;
            selectallCheckBox.Content = "(Select All)";
            selectallCheckBox.Click += delegate (object sender, RoutedEventArgs r)
            {
                CheckBox sAllBox = sender as CheckBox;
                OnSelectAllCheckCLicked(sAllBox);
            };
            StackForCheckBoxes.Children.Add(selectallCheckBox);
            foreach (String s in l)
            {
                if (!String.Equals(s, "(Blanks)"))
                {
                    CheckBox singleCheckBox = new CheckBox();
                    singleCheckBox.VerticalAlignment = VerticalAlignment.Center;
                    singleCheckBox.Margin = new Thickness(2);
                    singleCheckBox.Content = s;
                    singleCheckBox.IsChecked = true;
                    if (checkOnFilteredValueNeeded)
                    {
                        if (FilteredOutValues.Contains((String)s))
                        {
                            singleCheckBox.IsChecked = false;
                        }
                        else
                        {
                            singleCheckBox.IsChecked = true;
                        }
                        singleCheckBox.Checked += ActOnCheckEvent;
                        singleCheckBox.Unchecked += ActOnUnCheckEvent;
                    }
                    else
                    {
                        singleCheckBox.Checked += ActOnCheckEventInSearch;
                        singleCheckBox.Unchecked += ActOnUnCheckEventInSearch;
                    }
                    StackForCheckBoxes.Children.Add(singleCheckBox);
                    ItemsCheckBoxes.Add(singleCheckBox);
                }
                else
                {
                    isBlankFound = true;
                }
            }
            if (isBlankFound)
            {
                CheckBox blankCheckBox = new CheckBox();
                blankCheckBox.VerticalAlignment = VerticalAlignment.Center;
                blankCheckBox.Margin = new Thickness(2);
                blankCheckBox.Content = "(Blanks)";
                blankCheckBox.IsChecked = true;
                if (checkOnFilteredValueNeeded)
                {
                    if (FilteredOutValues.Contains("(Blanks)"))
                    {
                        blankCheckBox.IsChecked = false;
                    }
                    else
                    {
                        blankCheckBox.IsChecked = true;
                    }
                    blankCheckBox.Checked += ActOnCheckEvent;
                    blankCheckBox.Unchecked += ActOnUnCheckEvent;
                }
                else
                {
                    blankCheckBox.Checked += ActOnCheckEventInSearch;
                    blankCheckBox.Unchecked += ActOnUnCheckEventInSearch;
                }
                StackForCheckBoxes.Children.Add(blankCheckBox);
                ItemsCheckBoxes.Add(blankCheckBox);
            }
            ViewerForCheckBoxes.Content = StackForCheckBoxes;
            BorderForStackChecks.Child = ViewerForCheckBoxes;
        }

        void CreatePopUpOnToggled()
        {
            FilteredOutValues.Clear();
            SearchedListResult.Clear();
            TemporaryFilteredOutValues.Clear();
            InSearchTemporaryFilteredOutValues.Clear();
            IList DataSourceForPopUp = TotalDataSource.ToList();
            ListCollectionView viewForPopUp = new ListCollectionView(DataSourceForPopUp);
            Dictionary<String, Predicate<T>> dictionaryForPopUp = new Dictionary<String, Predicate<T>>();
            foreach (var item in DictionaryFilter)
            {
                var arrayS = item.Key.Split('_');
                if (!arrayS[0].Equals(HeaderTextNoUnderscore))
                {
                    dictionaryForPopUp.Add(item.Key, item.Value);
                }
            }
            viewForPopUp.Filter = (obj) =>
            {
                T c = (T)obj;
                return dictionaryForPopUp.Values.Aggregate(true, (prevValue, predicate) => prevValue && predicate(c));
            };
            PopUpFilterValues = viewForPopUp.Cast<T>().Select(o => o.GetValueOfField(HeaderText)).Distinct().OrderBy(o => o).Cast<String>().ToList();
            int indexOfBlank = PopUpFilterValues.IndexOf(String.Empty);
            if (indexOfBlank != -1)
            {
                PopUpFilterValues.Remove(String.Empty);
                PopUpFilterValues.Insert(indexOfBlank, "(Blanks)");
            }
            ListCollectionView viewForPopUpToTick = new ListCollectionView(DataSourceForPopUp);
            viewForPopUpToTick.Filter = (obj) =>
            {
                T c = (T)obj;
                return DictionaryFilter.Values.Aggregate(true, (prevValue, predicate) => prevValue && predicate(c));
            };
            List<String> PopUpFilterValuesToTick = viewForPopUpToTick.Cast<T>().Select(o => o.GetValueOfField(HeaderText)).Distinct().OrderBy(o => o).Cast<String>().ToList();
            int indexOfBlankToTick = PopUpFilterValuesToTick.IndexOf(String.Empty);
            if (indexOfBlankToTick != -1)
            {
                PopUpFilterValuesToTick.Remove(String.Empty);
                PopUpFilterValuesToTick.Insert(indexOfBlankToTick, "(Blanks)");
            }
            TemporaryFilteredOutValues = new ObservableCollection<String>();
            if (PopUpFilterValues.Count != PopUpFilterValuesToTick.Count)
            {
                foreach (String s in PopUpFilterValues)
                {
                    if (!PopUpFilterValuesToTick.Contains(s))
                    {
                        FilteredOutValues.Add(s);
                        TemporaryFilteredOutValues.Add(s);
                    }
                }
            }
            Grid GlobalGridInPopUp = new Grid();
            ColumnDefinition PupUpGridColumnDefinition = new ColumnDefinition();
            PupUpGridColumnDefinition.Width = new GridLength(1, GridUnitType.Star);
            RowDefinition PopUpGridRowDefinition0 = new RowDefinition();
            RowDefinition PopUpGridRowDefinition1 = new RowDefinition();
            RowDefinition PopUpGridRowDefinition2 = new RowDefinition();
            RowDefinition PopUpGridRowDefinition3 = new RowDefinition();
            PopUpGridRowDefinition0.Height = new GridLength(1, GridUnitType.Auto);
            PopUpGridRowDefinition1.Height = new GridLength(1, GridUnitType.Star);
            PopUpGridRowDefinition2.Height = new GridLength(1, GridUnitType.Auto);
            PopUpGridRowDefinition3.Height = new GridLength(1, GridUnitType.Auto);
            GlobalGridInPopUp.ColumnDefinitions.Add(PupUpGridColumnDefinition);
            GlobalGridInPopUp.RowDefinitions.Add(PopUpGridRowDefinition0);
            GlobalGridInPopUp.RowDefinitions.Add(PopUpGridRowDefinition1);
            GlobalGridInPopUp.RowDefinitions.Add(PopUpGridRowDefinition2);
            GlobalGridInPopUp.RowDefinitions.Add(PopUpGridRowDefinition3);
            Grid SearchGrid = new Grid();
            RowDefinition SearchGridRowDefinition = new RowDefinition();
            SearchGridRowDefinition.Height = new GridLength(1, GridUnitType.Auto);
            ColumnDefinition SearchGridColumnDefinition0 = new ColumnDefinition();
            ColumnDefinition SearchGridColumnDefinition1 = new ColumnDefinition();
            SearchGridColumnDefinition0.Width = new GridLength(1, GridUnitType.Auto);
            SearchGridColumnDefinition1.Width = new GridLength(1, GridUnitType.Star);
            SearchGrid.RowDefinitions.Add(SearchGridRowDefinition);
            SearchGrid.ColumnDefinitions.Add(SearchGridColumnDefinition0);
            SearchGrid.ColumnDefinitions.Add(SearchGridColumnDefinition1);
            TextBox SearchBox = new TextBox();
            SearchBox.MinWidth = 135;
            SearchBox.VerticalAlignment = VerticalAlignment.Center;
            SearchBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            SearchBox.Margin = new Thickness(5);
            SearchBox.TextChanged += OnSearchTextChanged;
            TextBlock searchTextBlock = new TextBlock();
            searchTextBlock.IsHitTestVisible = false;
            searchTextBlock.Text = "Search...";
            searchTextBlock.VerticalAlignment = VerticalAlignment.Center;
            searchTextBlock.HorizontalAlignment = HorizontalAlignment.Left;
            searchTextBlock.Margin = new Thickness(10, 0, 0, 0);
            searchTextBlock.Foreground = new SolidColorBrush(Colors.DarkGray);
            Style textBlockStyle = new Style(typeof(TextBlock));
            textBlockStyle.Setters.Add(new Setter(TextBlock.VisibilityProperty, Visibility.Collapsed));
            DataTrigger textBlockDataTrigger = new DataTrigger();
            textBlockDataTrigger.Value = "";
            Binding textBlockBinding = new Binding("Text");
            textBlockBinding.Source = SearchBox;
            textBlockDataTrigger.Binding = textBlockBinding;
            textBlockDataTrigger.Setters.Add(new Setter(TextBlock.VisibilityProperty, Visibility.Visible));
            textBlockStyle.Triggers.Add(textBlockDataTrigger);
            searchTextBlock.Style = textBlockStyle;
            Grid.SetRow(searchTextBlock, 0);
            Grid.SetColumn(searchTextBlock, 0);
            Grid.SetRow(SearchBox, 0);
            Grid.SetColumn(SearchBox, 1);
            SearchGrid.Children.Add(searchTextBlock);
            SearchGrid.Children.Add(SearchBox);
            StackPanel StackForButtons = new StackPanel();
            StackForButtons.Orientation = Orientation.Horizontal;
            StackForButtons.HorizontalAlignment = HorizontalAlignment.Right;
            StackForButtons.VerticalAlignment = VerticalAlignment.Bottom;
            OkButton = new Button();
            OkButton.Tag = "OK";
            OkButton.Content = "   Ok   ";
            OkButton.Click += ActOnOkCancelPressed;
            OkButton.Margin = new Thickness(5, 5, 5, 5);
            Button cancelButton = new Button();
            cancelButton.Tag = "CANCEL";
            cancelButton.Content = "   Cancel   ";
            cancelButton.Click += ActOnOkCancelPressed;
            cancelButton.Margin = new Thickness(5, 5, 5, 5);
            StackForButtons.Children.Add(OkButton);
            StackForButtons.Children.Add(cancelButton);
            Thumb PopUpResizeThumb = new Thumb();
            ControlTemplate ThumbTemplate = new ControlTemplate(typeof(Thumb));
            var imageThumb = new FrameworkElementFactory(typeof(Image));
            imageThumb.SetValue(Image.SourceProperty, ImagePopBitmap);
            ThumbTemplate.VisualTree = imageThumb;
            PopUpResizeThumb.Template = ThumbTemplate;
            PopUpResizeThumb.DragStarted += OnDragThumbStarted;
            PopUpResizeThumb.DragDelta += OnDragThumbDelta;
            PopUpResizeThumb.DragCompleted += OnDragThumbCompleted;
            PopUpResizeThumb.VerticalAlignment = VerticalAlignment.Bottom;
            PopUpResizeThumb.HorizontalAlignment = HorizontalAlignment.Right;
            PopUpResizeThumb.Height = 10;
            BorderForStackChecks = new Border();
            BorderForStackChecks.BorderBrush = Brushes.Black;
            BorderForStackChecks.BorderThickness = new Thickness(0.5);
            ViewerForCheckBoxes = new ScrollViewer();
            ViewerForCheckBoxes.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            ViewerForCheckBoxes.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            if (PopUpFilterValues.Count > 0)
            {
                PopulateStackWithCheckBoxes(PopUpFilterValues, true);
            }
            Grid.SetRow(SearchGrid, 0);
            Grid.SetColumn(SearchGrid, 0);
            Grid.SetRow(BorderForStackChecks, 1);
            Grid.SetColumn(BorderForStackChecks, 0);
            Grid.SetRow(StackForButtons, 2);
            Grid.SetColumn(StackForButtons, 0);
            Grid.SetRow(PopUpResizeThumb, 3);
            Grid.SetColumn(PopUpResizeThumb, 0);
            GlobalGridInPopUp.Children.Add(SearchGrid);
            GlobalGridInPopUp.Children.Add(BorderForStackChecks);
            GlobalGridInPopUp.Children.Add(StackForButtons);
            GlobalGridInPopUp.Children.Add(PopUpResizeThumb);
            Border aroundGlobalStackBorder = new Border();
            aroundGlobalStackBorder.Background = new SolidColorBrush(Colors.White);
            aroundGlobalStackBorder.BorderBrush = new SolidColorBrush(Colors.Black);
            aroundGlobalStackBorder.BorderThickness = new Thickness(1);
            aroundGlobalStackBorder.Child = GlobalGridInPopUp;
            HeaderPopUp = new Popup();
            HeaderPopUp.Closed += delegate (object sender, EventArgs ea)
            {
                if (!HeaderToggle.IsPressed)
                {
                    HeaderToggle.IsChecked = false;
                }
            };
            HeaderPopUp.PlacementTarget = this;
            HeaderPopUp.AllowsTransparency = false;
            HeaderPopUp.Child = aroundGlobalStackBorder;
            HeaderPopUp.Placement = PlacementMode.Bottom;
            HeaderPopUp.Width = 200;
            HeaderPopUp.MinHeight = 200;
            HeaderPopUp.MinWidth = 200;
            HeaderPopUp.Height = 200;
            HeaderPopUp.HorizontalOffset = this.ActualWidth - HeaderPopUp.MinWidth;
            HeaderPopUp.PopupAnimation = PopupAnimation.Slide;
            HeaderPopUp.StaysOpen = false;
        }


        // Helper method: "FilterCandidates", aggregate the predicates in the dictionary to return the boolean for the filter
        // Helper method: "RemoveDictionaryFilter", remove filter predicate from dictionary
        // Helper method: "AddFilterAndRefresh", add filter predicate to dictionary
        // Helper method: "FilterOnValue", create single predicate based on selcted checkbox
        // Helper method: "LogicalOr", logical "or" to apply all the predicates

        bool FilterCandidates(object obj)
        {
            T c = (T)obj;
            return DictionaryFilter.Values.Aggregate(true, (prevValue, predicate) => prevValue && predicate(c));
        }

        void RemoveDictionaryFilter()
        {
            try
            {
                if (DictionaryFilter.Remove(DictionaryFilter.Where(o => o.Key.Contains(HeaderTextNoUnderscore)).ElementAt(0).Key))
                {
                    DataCollectionView.Refresh();
                    HeaderToggle.Content = ImageArrowBlack;
                }
            }
            catch (Exception)
            {

            }
        }

        void AddFilterAndRefresh(string name, Predicate<T> predicate)
        {
            if (DictionaryFilter.Where(o => o.Key.Contains(HeaderTextNoUnderscore)).Count() > 0)
            {
                DictionaryFilter.Remove(DictionaryFilter.Where(o => o.Key.Contains(HeaderTextNoUnderscore)).ElementAt(0).Key);
                DictionaryFilter.Add(name, predicate);
            }
            else
            {
                DictionaryFilter.Add(name, predicate);
            }
            DataCollectionView.Refresh();
        }

        void FilterOnValue()
        {
            if (((CheckBox)StackForCheckBoxes.Children[0]).IsChecked == true && !(SearchedListResult.Count > 0))
            {
                RemoveDictionaryFilter();
            }
            else
            {
                List<Predicate<T>> lpt = new List<Predicate<T>>();
                for (int i = 0; i < PopUpFilterValues.Count; i++)
                {
                    String value = PopUpFilterValues.ElementAt(i);
                    if (!FilteredOutValues.Contains(value))
                    {
                        Predicate<T> function = delegate (T t) {
                            if (value.Equals("(Blanks)"))
                            {
                                value = String.Empty;
                            }
                            return t.GetValueOfField(HeaderText).Equals(value);
                        };
                        lpt.Add(function);
                    }
                }
                String insertionTimeString = (DateTime.UtcNow - (new DateTime(1900, 01, 01))).TotalMilliseconds.ToString();
                String key = String.Concat(HeaderTextNoUnderscore, "_", insertionTimeString);
                AddFilterAndRefresh(key, LogicalOr(lpt));
            }
        }

        Predicate<E> LogicalOr<E>(List<Predicate<E>> predicates)
        {
            return delegate (E item)
            {
                foreach (Predicate<E> predicate in predicates)
                {
                    if (predicate(item))
                    {
                        return true;
                    }
                }
                return false;
            };
        }


        // Public method: Exposed to make possible removing all the filters applied to a column from outside

        /// <summary>
        ///  Remove all filters applied to this particular column
        /// </summary>
        public void RemoveFilter()
        {
            RemoveDictionaryFilter();
        }


        // Converters

        class ConvertCountToChecked : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                try
                {
                    if ((int)value == 0)
                    {
                        return true;
                    }
                    else if ((int)value == (int)parameter)
                    {
                        return false;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception e)
                {
                    throw new NotImplementedException(e.Message);
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        class ConvertCountToEnabled : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                try
                {
                    if ((int)value < (int)parameter)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    throw new NotImplementedException(e.Message);
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}