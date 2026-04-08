using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Launcher.UI.WPF.Helpers;

public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    public void AddRange(IEnumerable<T> collection)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));

        CheckReentrancy();
        
        foreach (var item in collection)
        {
            Items.Add(item); 
        }
        
        RaiseCollectionChanged();
    }
    
    public void ReplaceRange(IEnumerable<T> collection)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));

        CheckReentrancy();
        
        Items.Clear();
        foreach (var item in collection)
        {
            Items.Add(item); 
        }
        
        RaiseCollectionChanged();
    }

    private void RaiseCollectionChanged()
    {
        OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}