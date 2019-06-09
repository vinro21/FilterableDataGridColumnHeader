using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;

namespace com.rocco.vincenzo.csharp.controls.datamodels
{
    /// <summary>  
    ///  Abstract Class acting as model for the concrete one used by the FilterableDataGridColumnHeader
    /// </summary> 
    public abstract class BaseFilterableData : DynamicObject, INotifyPropertyChanged
    {
        private Dictionary<String, String> DataHeadersValuesPairs;
        public static List<String> HeadersCollection = new List<String>();
        private const String HEADER = "Header";
        private const String INVALIDHEADER = "*#Invalid Header#*";
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>  
        ///  Dictionary Keys are the names of the Properties of the concrete Class. If a blank key is found when a specific derived class is instantiated for the first time then the same is replaced with 
        ///  a placeholder as "Header + (i + 1)" where i is the index. If keys count of the dictionary used for one subsequent instance of a specific derived class differs from the count of the first instance
        ///  then all the values of that particular instance are set to "*#Invalid Header#*" placeholder.
        /// </summary>
        public BaseFilterableData(Dictionary<String, String> propertiesNamesAndValues)
        {
            DataHeadersValuesPairs = new Dictionary<String, String>();
            if (HeadersCollection.Count == 0)
            {
                for (int i = 0; i < propertiesNamesAndValues.Keys.Count; i++)
                {
                    if (String.IsNullOrWhiteSpace(propertiesNamesAndValues.Keys.ElementAt(i)))
                    {
                        DataHeadersValuesPairs.Add(HEADER + (i + 1), propertiesNamesAndValues[propertiesNamesAndValues.Keys.ElementAt(i)]);
                        HeadersCollection.Add(HEADER + (i + 1));
                    }
                    else
                    {
                        DataHeadersValuesPairs.Add(propertiesNamesAndValues.Keys.ElementAt(i), propertiesNamesAndValues[propertiesNamesAndValues.Keys.ElementAt(i)]);
                        HeadersCollection.Add(propertiesNamesAndValues.Keys.ElementAt(i));
                    }
                }
            }
            else
            {
                if (HeadersCollection.Count != propertiesNamesAndValues.Keys.Count)
                {
                    for (int i = 0; i < HeadersCollection.Count; i++)
                    {
                        DataHeadersValuesPairs.Add(HeadersCollection.ElementAt(i), INVALIDHEADER);
                    }
                }
                else
                {
                    for (int i = 0; i < propertiesNamesAndValues.Keys.Count; i++)
                    {
                        if (HeadersCollection.Contains(propertiesNamesAndValues.Keys.ElementAt(i)))
                        {
                            DataHeadersValuesPairs.Add(propertiesNamesAndValues.Keys.ElementAt(i), propertiesNamesAndValues[propertiesNamesAndValues.Keys.ElementAt(i)]);
                        }
                        else
                        {
                            if (String.IsNullOrWhiteSpace(propertiesNamesAndValues.Keys.ElementAt(i)) && HeadersCollection.Any(x => x.Contains(HEADER)))
                            {
                                DataHeadersValuesPairs.Add(HeadersCollection.Find(x => x.Contains(HEADER)), propertiesNamesAndValues[propertiesNamesAndValues.Keys.ElementAt(i)]);
                            }
                            else
                            {
                                DataHeadersValuesPairs.Add(HeadersCollection.ElementAt(i), INVALIDHEADER);
                            }
                        }
                    }
                }
            }
        }

        public override sealed bool TryGetMember(GetMemberBinder binder, out Object result)
        {
            String foundString;
            bool foundResult;
            foundResult = DataHeadersValuesPairs.TryGetValue(binder.Name, out foundString);
            result = (Object)foundString;
            return foundResult;
        }

        public override sealed bool TrySetMember(SetMemberBinder binder, Object value)
        {
            DataHeadersValuesPairs[binder.Name] = (String)value;
            return true;
        }

        /// <summary>  
        ///  Return the value of a particular Property in the Class given its name
        /// </summary>
        public String GetValueOfField(String name)
        {
            String value;
            DataHeadersValuesPairs.TryGetValue(name, out value);
            return value;
        }

        /// <summary>  
        ///  Set the value of a particular Property in the Class given its name and value to use
        /// </summary>
        public void SetValueOfField(String name, String value)
        {
            if (DataHeadersValuesPairs.Keys.Contains(name, StringComparer.InvariantCulture))
            {
                DataHeadersValuesPairs[name] = value;
                NotifyPropertyChanged();
            }
        }

        internal void ModifyHeaderName(String oldName, String newName)
        {
            if (HeadersCollection.Contains(oldName) && (String.IsNullOrWhiteSpace(newName) || HeadersCollection.Contains(newName)))
            {
                throw new InvalidOperationException("New name supplied is empty, null or with white spaces only, or already exists in this class");
            }
            else
            {
                if (HeadersCollection.Contains(oldName))
                {
                    HeadersCollection.Remove(oldName);
                    HeadersCollection.Add(newName);
                }
                var currentValue = DataHeadersValuesPairs[oldName];
                DataHeadersValuesPairs.Add(newName, currentValue);
            }
        }
    }
}