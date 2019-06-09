## FilterableDataGridColumnHeader

It is a **C#** control to provide filtering capabilities to a **WPF** (Windows Presentation Foundation) **DataGrid** column header, similarly to those available in MS Excel.

The control itself works in pair with the abstract data class "BaseFilterableData" which limits the possible types of the generic parameter T: 

BaseFilterableData derives from DynamicObject, which allows the column headers name (basically the class Properties names) to be defined on a project by project basis 

(possibly at runtime even), whenever a concrete derived class (such as "Sample Class" in below example) is instantiated. BaseFilterableData tries to handle conflict

situations (blank header/property names, dictionaries of different lenght used at derived class instantiations, etc) gracefully as far as possible.

The three parameters in the FilterableDataGridColumnHeader contructor are 1) a String which will be used as the text shown in the header, 2) an IList(T) which is also used

for the "ItemsSource" parameter of the DataGrid, 3) an ICollectionView derived from the ItemsSource.

The control offers also several public properties backed up by DependencyProperty ones: 

The most important one is the bool "Filter" which makes the filtering option appear/disappear, while "HeaderText" returns the text displayed in the header or let

set a new one (InvalidOperationException is thrown though if null/blank or a text already present in another header in the DataGrid is used)


## Example Usage

```
using com.rocco.vincenzo.csharp.controls.datamodels;
using com.rocco.vincenzo.csharp.controls.primitives;

Dictionary<String, String> HeadersValuesDictionary = new Dictionary<String, String>();
HeadersValuesDictionary.Add("First Name", "John");
HeadersValuesDictionary.Add("Last Name", "Doe");
HeadersValuesDictionary.Add("Country", "USA");
HeadersValuesDictionary.Add("City", "New Orleans"); 

ObservableCollection<SampleClass> AppListSampleClass = new ObservableCollection<SampleClass>();
AppListSampleClass.Add(new SampleClass(HeadersValuesDictionary));    // "SampleClass" extends the abstract DynamicObject "BaseFilterableData"

DataGrid AppDataGrid = new DataGrid();
AppDataGrid.ItemsSource = AppListSampleClass;
ICollectionView AppListDataListView = CollectionViewSource.GetDefaultView(appListSampleClass);

for (int i = 0; i < SampleClass.HeadersCollections.Count(); i++)
{
	DataGridTextColumn massGridTemplateColumn = new DataGridTextColumn();
        FilteredDataGridColumnHeader<SampleClass> FilterableHeader = new FilterableDataGridColumnHeader<SampleClass>(SampleClass.HeadersCollections.ElementAt(i), AppListSampleClass, AppListDataListView);
        MassGridTemplateColumn.Header = FilterableHeader;
        Binding ColumnBinding = new Binding(SampleClass.HeadersCollections.ElementAt(i));
        MassGridTemplateColumn.Binding = ColumnBinding;
        MassGridTemplateColumn.IsReadOnly = true;
        AppDataGrid.Columns.Add(MassGridTemplateColumn);
}
```

## Screenshots

![Filterable1](https://user-images.githubusercontent.com/51540377/59163613-9572c400-8b03-11e9-98a9-eb3df2c9eb79.PNG)

![Filterable2](https://user-images.githubusercontent.com/51540377/59163622-b0ddcf00-8b03-11e9-8a7d-ffd4b8626b27.PNG)

## Motivation

Having worked more times with the DataGrid control, I started wondering on the possibility of having a filterable header to help modifying the data displayed in the application:

this is the solution I came out with, which works enough with data values of type String.


## Credits

Icons taken from https://www.flaticon.com (black and red one by Google), can be replaced with other ones.


## License

[GNU GPLv3](https://www.gnu.org/licenses/gpl-3.0-standalone.html)