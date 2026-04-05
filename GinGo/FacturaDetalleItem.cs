using System.ComponentModel;

namespace GinGo;

public class FacturaDetalleItem : INotifyPropertyChanged
{
    private string _codigo = string.Empty;
    private string _descripcion = string.Empty;
    private string _unidad = "NIU";
    private string _cantidad = "1";
    private string _precioTotal = "0.00";
    private string _tipoIgv = "Gravado (18%)";

    public string Codigo
    {
        get => _codigo;
        set
        {
            if (_codigo == value)
            {
                return;
            }

            _codigo = value;
            OnPropertyChanged(nameof(Codigo));
        }
    }

    public string Descripcion
    {
        get => _descripcion;
        set
        {
            if (_descripcion == value)
            {
                return;
            }

            _descripcion = value;
            OnPropertyChanged(nameof(Descripcion));
        }
    }

    public string Unidad
    {
        get => _unidad;
        set
        {
            if (_unidad == value)
            {
                return;
            }

            _unidad = value;
            OnPropertyChanged(nameof(Unidad));
        }
    }

    public string Cantidad
    {
        get => _cantidad;
        set
        {
            if (_cantidad == value)
            {
                return;
            }

            _cantidad = value;
            OnPropertyChanged(nameof(Cantidad));
        }
    }

    public string PrecioTotal
    {
        get => _precioTotal;
        set
        {
            if (_precioTotal == value)
            {
                return;
            }

            _precioTotal = value;
            OnPropertyChanged(nameof(PrecioTotal));
        }
    }

    public string TipoIgv
    {
        get => _tipoIgv;
        set
        {
            if (_tipoIgv == value)
            {
                return;
            }

            _tipoIgv = value;
            OnPropertyChanged(nameof(TipoIgv));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
