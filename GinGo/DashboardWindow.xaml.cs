using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace GinGo;

public partial class DashboardWindow : Window, INotifyPropertyChanged
{
    public PlotModel DailyModel { get; set; }
    public PlotModel MonthlyModel { get; set; }
    public PlotModel TypeModel { get; set; }
    public ObservableCollection<FacturaDetalleItem> FacturaDetalles { get; } = new();
    public ObservableCollection<string> TipoIgvOptions { get; } =
    [
        "Gravado (18%)",
        "Exonerado",
        "Inafecto"
    ];

    private string _facturaSubtotalTexto = "S/ 0.00";
    private string _facturaIgvTexto = "S/ 0.00";
    private string _facturaTotalTexto = "S/ 0.00";

    public string FacturaSubtotalTexto
    {
        get => _facturaSubtotalTexto;
        set
        {
            if (_facturaSubtotalTexto == value)
            {
                return;
            }

            _facturaSubtotalTexto = value;
            OnPropertyChanged(nameof(FacturaSubtotalTexto));
        }
    }

    public string FacturaIgvTexto
    {
        get => _facturaIgvTexto;
        set
        {
            if (_facturaIgvTexto == value)
            {
                return;
            }

            _facturaIgvTexto = value;
            OnPropertyChanged(nameof(FacturaIgvTexto));
        }
    }

    public string FacturaTotalTexto
    {
        get => _facturaTotalTexto;
        set
        {
            if (_facturaTotalTexto == value)
            {
                return;
            }

            _facturaTotalTexto = value;
            OnPropertyChanged(nameof(FacturaTotalTexto));
        }
    }

    public DashboardWindow(string ruc, string username)
    {
        InitializeComponent();

        RucTextBlock.Text = $"RUC: {ruc} | Usuario: {username}";

        // Configuración de Gráficos de Línea (Días)
        DailyModel = new PlotModel { Background = OxyColors.Transparent, PlotAreaBorderThickness = new OxyThickness(0) };
        var dailyCategoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Key = "Days" };
        dailyCategoryAxis.Labels.AddRange(new[] { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" });
        dailyCategoryAxis.AxislineStyle = LineStyle.Solid;
        DailyModel.Axes.Add(dailyCategoryAxis);
        
        var valueAxis = new LinearAxis { Position = AxisPosition.Left, MinimumPadding = 0, AbsoluteMinimum = 0, MajorGridlineStyle = LineStyle.Solid, MajorGridlineColor = OxyColor.Parse("#E2E8F0") };
        DailyModel.Axes.Add(valueAxis);

        var lineSeries = new LineSeries
        {
            Color = OxyColor.Parse("#3B82F6"),
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerStroke = OxyColor.Parse("#3B82F6"),
            MarkerFill = OxyColors.White,
            StrokeThickness = 2
        };
        var dailyValues = new[] { 2, 5, 4, 2, 6, 8, 12 };
        for (int i = 0; i < dailyValues.Length; i++)
        {
            lineSeries.Points.Add(new DataPoint(i, dailyValues[i]));
        }
        DailyModel.Series.Add(lineSeries);

        // Configuración de Gráficos de Barras (Meses)
        MonthlyModel = new PlotModel { Background = OxyColors.Transparent, PlotAreaBorderThickness = new OxyThickness(0) };
        var monthCategoryAxis = new CategoryAxis { Position = AxisPosition.Bottom };
        monthCategoryAxis.Labels.AddRange(new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul" });
        monthCategoryAxis.AxislineStyle = LineStyle.Solid;
        MonthlyModel.Axes.Add(monthCategoryAxis);
        
        var monthValueAxis = new LinearAxis { Position = AxisPosition.Left, MinimumPadding = 0, AbsoluteMinimum = 0, MajorGridlineStyle = LineStyle.Solid, MajorGridlineColor = OxyColor.Parse("#E2E8F0") };
        MonthlyModel.Axes.Add(monthValueAxis);

        var barSeries = new BarSeries
        {
            FillColor = OxyColor.Parse("#60A5FA"),
            StrokeColor = OxyColors.Transparent
        };
        var monthValues = new[] { 45, 60, 55, 80, 105, 90, 145 };
        for (int i = 0; i < monthValues.Length; i++)
        {
            barSeries.Items.Add(new BarItem(monthValues[i], i));
        }
        MonthlyModel.Series.Add(barSeries);

        // Configuración de Gráfico de Pastel (Tipos)
        TypeModel = new PlotModel { Background = OxyColors.Transparent, PlotAreaBorderThickness = new OxyThickness(0) };
        var pieSeries = new PieSeries
        {
            StrokeThickness = 2.0,
            InsideLabelPosition = 0.8,
            AngleSpan = 360,
            StartAngle = 0
        };
        pieSeries.Slices.Add(new PieSlice("Facturas", 80) { Fill = OxyColor.Parse("#3B82F6") });
        pieSeries.Slices.Add(new PieSlice("Recibos", 45) { Fill = OxyColor.Parse("#10B981") });
        pieSeries.Slices.Add(new PieSlice("Boletas", 20) { Fill = OxyColor.Parse("#F59E0B") });
        TypeModel.Series.Add(pieSeries);

        AgregarDetalleFactura();
        RecalcularFacturaTotales();
        DataContext = this;
    }

    private void Menu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string viewName)
        {
            DashboardView.Visibility = Visibility.Collapsed;
            ValidarUsuarioView.Visibility = Visibility.Collapsed;
            GenerarIngresoView.Visibility = Visibility.Collapsed;

            if (viewName == "DashboardView") DashboardView.Visibility = Visibility.Visible;
            if (viewName == "ValidarUsuarioView") ValidarUsuarioView.Visibility = Visibility.Visible;
            if (viewName == "GenerarIngresoView") GenerarIngresoView.Visibility = Visibility.Visible;
        }
    }

    private async void ValidarSunat_Click(object sender, RoutedEventArgs e)
    {
        var ruc = ValidarRucTextBox.Text.Trim();
        var user = ValidarUsuarioSolTextBox.Text.Trim();
        var pass = ValidarPassSolBox.Password;

        if (string.IsNullOrWhiteSpace(ruc) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            MessageBox.Show("Por favor completa todos los campos.", "Validar Usuario", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (user != "ADMINSOL" || pass != "Sol123!")
        {
            MessageBox.Show("Usuario SOL o contraseña SOL inválidos. Usa ADMINSOL y Sol123!.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (ruc.Length != 11)
        {
            MessageBox.Show("El RUC debe tener 11 dígitos.", "Validar Usuario", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (sender is Button botonValidar)
        {
            botonValidar.IsEnabled = false;
            botonValidar.Content = "Validando...";
        }

        try
        {
            var resultado = await ValidarRucSunatAsync(ruc);

            if (!resultado.EsValido)
            {
                MessageBox.Show(resultado.Mensaje, "Validación SUNAT", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RucTextBlock.Text = $"RUC: {ruc} | Usuario: {user}";

            MessageBox.Show($"RUC validado correctamente.\nRazón social: {resultado.RazonSocial}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            BtnValidarUsuario.Visibility = Visibility.Collapsed;
            BtnGenerarIngreso.Visibility = Visibility.Visible;

            DashboardView.Visibility = Visibility.Collapsed;
            ValidarUsuarioView.Visibility = Visibility.Collapsed;
            GenerarIngresoView.Visibility = Visibility.Visible;

            FacturaFormContainer.Visibility = Visibility.Visible;
            BoletaFormContainer.Visibility = Visibility.Collapsed;
        }
        finally
        {
            if (sender is Button boton)
            {
                boton.IsEnabled = true;
                boton.Content = "Validar Credenciales";
            }
        }
    }

    private void BackToSunatValidation_Click(object sender, RoutedEventArgs e)
    {
        // Limpiamos los campos
        ValidarRucTextBox.Text = string.Empty;
        ValidarUsuarioSolTextBox.Text = string.Empty;
        ValidarPassSolBox.Password = string.Empty;

        // Ocultamos la pestaña de Generar Ingreso y mostramos Validar Usuario
        BtnGenerarIngreso.Visibility = Visibility.Collapsed;
        BtnValidarUsuario.Visibility = Visibility.Visible;

        // Cambiamos la vista
        DashboardView.Visibility = Visibility.Collapsed;
        GenerarIngresoView.Visibility = Visibility.Collapsed;
        ValidarUsuarioView.Visibility = Visibility.Visible;
    }

    private void ShowFacturaForm_Click(object sender, RoutedEventArgs e)
    {
        FacturaFormContainer.Visibility = Visibility.Visible;
        BoletaFormContainer.Visibility = Visibility.Collapsed;
    }

    private void ShowBoletaForm_Click(object sender, RoutedEventArgs e)
    {
        FacturaFormContainer.Visibility = Visibility.Collapsed;
        BoletaFormContainer.Visibility = Visibility.Visible;
    }

    private void AgregarDetalleFactura_Click(object sender, RoutedEventArgs e)
    {
        AgregarDetalleFactura();
    }

    private void LimpiarFactura_Click(object sender, RoutedEventArgs e)
    {
        foreach (var detalle in FacturaDetalles)
        {
            detalle.PropertyChanged -= FacturaDetalle_PropertyChanged;
        }

        FacturaDetalles.Clear();
        AgregarDetalleFactura();
        RecalcularFacturaTotales();
    }

    private async void EmitirFactura_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            btn.IsEnabled = false;
            btn.Content = "Procesando...";
        }

        try
        {
            // Validar que haya datos
            if (string.IsNullOrWhiteSpace(FacturaDocNumeroTextBox.Text) || string.IsNullOrWhiteSpace(FacturaRazonSocialTextBox.Text))
            {
                MessageBox.Show("Por favor completa los datos del cliente.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (FacturaDetalles.Count == 0)
            {
                MessageBox.Show("Debes agregar al menos un producto.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Preparar el payload para el bridge
            var payload = new
            {
                action = "sendInvoice",
                ruc = "20000000001", // Debería venir de la configuración
                usuarioSol = "MODDATOS",
                claveSol = "moddatos",
                tipoDoc = "01", // Factura
                serie = "F001",
                correlativo = "1",
                cliente = new
                {
                    tipoDoc = "6",
                    numDoc = FacturaDocNumeroTextBox.Text.Trim(),
                    razonSocial = FacturaRazonSocialTextBox.Text.Trim()
                },
                details = FacturaDetalles.Select(d => new
                {
                    codigo = d.Codigo,
                    descripcion = d.Descripcion,
                    cantidad = ParseDecimal(d.Cantidad),
                    unidad = "NIU",
                    precioUnitario = ParseDecimal(d.PrecioTotal) / (ParseDecimal(d.Cantidad) == 0 ? 1 : ParseDecimal(d.Cantidad)),
                    valorUnitario = (ParseDecimal(d.PrecioTotal) / 1.18m) / (ParseDecimal(d.Cantidad) == 0 ? 1 : ParseDecimal(d.Cantidad)),
                    baseIgv = (ParseDecimal(d.PrecioTotal) / 1.18m),
                    igv = ParseDecimal(d.PrecioTotal) - (ParseDecimal(d.PrecioTotal) / 1.18m),
                    totalImpuestos = ParseDecimal(d.PrecioTotal) - (ParseDecimal(d.PrecioTotal) / 1.18m),
                    valorVenta = (ParseDecimal(d.PrecioTotal) / 1.18m),
                    tipAfeIgv = d.TipoIgv == "Gravado (18%)" ? "10" : "20"
                }).ToList(),
                totales = new
                {
                    totalVenta = ParseDecimal(FacturaTotalTexto.Replace("S/ ", "")),
                    totalImpuestos = ParseDecimal(FacturaIgvTexto.Replace("S/ ", "")),
                    igv = ParseDecimal(FacturaIgvTexto.Replace("S/ ", "")),
                    operGravadas = ParseDecimal(FacturaSubtotalTexto.Replace("S/ ", "")),
                    subTotal = ParseDecimal(FacturaTotalTexto.Replace("S/ ", ""))
                },
                montoLetras = "SON " + FacturaTotalTexto.Replace("S/ ", "") + " SOLES"
            };

            var response = await GreenterBridge.CallAsync(payload);

            if (response.Success)
            {
                MessageBox.Show($"Factura emitida correctamente.\nRespuesta SUNAT: {response.Message}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Si falla el bridge por falta de PHP o certificado, mostramos un mensaje amigable
                if (response.Message.Contains("php") || response.Message.Contains("No existe el certificado"))
                {
                    MessageBox.Show("El puente PHP (Greenter) no está configurado correctamente en este entorno.\n\n" + response.Message, "Información de Bridge", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"Error al emitir factura: {response.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (sender is Button btnVolver)
            {
                btnVolver.IsEnabled = true;
                btnVolver.Content = "Generar y Enviar a SUNAT";
            }
        }
    }

    private void LimpiarBoleta_Click(object sender, RoutedEventArgs e)
    {
        BoletaDocNumeroTextBox.Text = string.Empty;
        BoletaNombreTextBox.Text = string.Empty;
        BoletaDireccionTextBox.Text = string.Empty;
        // En un caso real, también limpiaríamos la lista de detalles de boleta si existiera
    }

    private async void EmitirBoleta_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            btn.IsEnabled = false;
            btn.Content = "Procesando...";
        }

        try
        {
            if (string.IsNullOrWhiteSpace(BoletaDocNumeroTextBox.Text) || string.IsNullOrWhiteSpace(BoletaNombreTextBox.Text))
            {
                MessageBox.Show("Por favor completa los datos del cliente.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Para boletas es similar, pero con serie B001 y tipo comprobante 03
            var payload = new
            {
                action = "sendInvoice",
                ruc = "20000000001",
                usuarioSol = "MODDATOS",
                claveSol = "moddatos",
                tipoDoc = "03", // Boleta
                serie = "B001",
                correlativo = "1",
                cliente = new
                {
                    tipoDoc = GetBoletaTipoDocCode(),
                    numDoc = BoletaDocNumeroTextBox.Text.Trim(),
                    razonSocial = BoletaNombreTextBox.Text.Trim()
                },
                details = new[]
                {
                    new
                    {
                        codigo = BoletaDetalleCodigoTextBox.Text.Trim(),
                        descripcion = BoletaDetalleDescripcionTextBox.Text.Trim(),
                        cantidad = ParseDecimal(BoletaDetalleCantidadTextBox.Text),
                        unidad = "NIU",
                        precioUnitario = ParseDecimal(BoletaDetallePrecioUnitTextBox.Text) + (ParseDecimal(BoletaDetalleIgvTextBox.Text) / (ParseDecimal(BoletaDetalleCantidadTextBox.Text) == 0 ? 1 : ParseDecimal(BoletaDetalleCantidadTextBox.Text))),
                        valorUnitario = ParseDecimal(BoletaDetallePrecioUnitTextBox.Text) / (ParseDecimal(BoletaDetalleCantidadTextBox.Text) == 0 ? 1 : ParseDecimal(BoletaDetalleCantidadTextBox.Text)),
                        baseIgv = ParseDecimal(BoletaDetallePrecioUnitTextBox.Text),
                        igv = ParseDecimal(BoletaDetalleIgvTextBox.Text),
                        totalImpuestos = ParseDecimal(BoletaDetalleIgvTextBox.Text),
                        valorVenta = ParseDecimal(BoletaDetallePrecioUnitTextBox.Text),
                        tipAfeIgv = "10"
                    }
                },
                totales = new
                {
                    totalVenta = ParseDecimal(BoletaDetallePrecioUnitTextBox.Text) + ParseDecimal(BoletaDetalleIgvTextBox.Text),
                    totalImpuestos = ParseDecimal(BoletaDetalleIgvTextBox.Text),
                    igv = ParseDecimal(BoletaDetalleIgvTextBox.Text),
                    operGravadas = ParseDecimal(BoletaDetallePrecioUnitTextBox.Text),
                    subTotal = ParseDecimal(BoletaDetallePrecioUnitTextBox.Text) + ParseDecimal(BoletaDetalleIgvTextBox.Text)
                },
                montoLetras = "SON " + (ParseDecimal(BoletaDetallePrecioUnitTextBox.Text) + ParseDecimal(BoletaDetalleIgvTextBox.Text)).ToString("0.00") + " SOLES"
            };

            var response = await GreenterBridge.CallAsync(payload);

            if (response.Success)
            {
                MessageBox.Show($"Boleta emitida correctamente.\nRespuesta SUNAT: {response.Message}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if (response.Message.Contains("php") || response.Message.Contains("No existe el certificado"))
                {
                    MessageBox.Show("El puente PHP (Greenter) no está configurado correctamente en este entorno.\n\n" + response.Message, "Información de Bridge", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"Error al emitir boleta: {response.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (sender is Button btnVolver)
            {
                btnVolver.IsEnabled = true;
                btnVolver.Content = "Generar y Enviar a SUNAT";
            }
        }
    }

    private string GetBoletaTipoDocCode()
    {
        if (BoletaTipoDocComboBox.SelectedItem is ComboBoxItem item)
        {
            var content = item.Content.ToString();
            return content switch
            {
                "DNI" => "1",
                "RUC" => "6",
                "SIN DOC" => "0",
                _ => "1"
            };
        }
        return "1";
    }

    private void AgregarDetalleFactura()
    {
        var detalle = new FacturaDetalleItem();
        detalle.PropertyChanged += FacturaDetalle_PropertyChanged;
        FacturaDetalles.Add(detalle);
        RecalcularFacturaTotales();
    }

    private void FacturaDetalle_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RecalcularFacturaTotales();
    }

    private void RecalcularFacturaTotales()
    {
        decimal subtotal = 0m;
        decimal igv = 0m;

        foreach (var detalle in FacturaDetalles)
        {
            var precioTotal = ParseDecimal(detalle.PrecioTotal);

            if (detalle.TipoIgv == "Gravado (18%)")
            {
                var igvLinea = Math.Round(precioTotal * 0.18m, 2);
                igv += igvLinea;
                subtotal += precioTotal - igvLinea;
            }
            else
            {
                subtotal += precioTotal;
            }
        }

        var total = subtotal + igv;

        FacturaSubtotalTexto = FormatearMoneda(subtotal);
        FacturaIgvTexto = FormatearMoneda(igv);
        FacturaTotalTexto = FormatearMoneda(total);
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var resultado))
        {
            return resultado;
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out resultado))
        {
            return resultado;
        }

        var normalizado = value.Replace(',', '.');
        return decimal.TryParse(normalizado, NumberStyles.Any, CultureInfo.InvariantCulture, out resultado)
            ? resultado
            : 0m;
    }

    private static string FormatearMoneda(decimal value)
    {
        return string.Format(CultureInfo.InvariantCulture, "S/ {0:0.00}", value);
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ToggleTheme();
            
            // Actualizar colores de los gráficos según el tema
            var isDark = app.Resources.MergedDictionaries[0].Source.OriginalString.Contains("DarkTheme");
            var textColor = isDark ? OxyColors.White : OxyColor.Parse("#111827");
            var gridColor = isDark ? OxyColor.Parse("#374151") : OxyColor.Parse("#E2E8F0");

            UpdateModelTheme(DailyModel, textColor, gridColor);
            UpdateModelTheme(MonthlyModel, textColor, gridColor);
            UpdateModelTheme(TypeModel, textColor, gridColor);
        }
    }

    private void UpdateModelTheme(PlotModel model, OxyColor textColor, OxyColor gridColor)
    {
        model.TextColor = textColor;
        foreach (var axis in model.Axes)
        {
            axis.TextColor = textColor;
            axis.TicklineColor = textColor;
            axis.AxislineColor = textColor;
            if (axis.MajorGridlineStyle != LineStyle.None)
            {
                axis.MajorGridlineColor = gridColor;
            }
        }
        model.InvalidatePlot(true);
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        var loginWindow = new MainWindow();
        loginWindow.Show();
        this.Close();
    }

    private readonly string ApiToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJlbWFpbCI6ImFjdWFyaW9qYXJhZ29uemFsb0BnbWFpbC5jb20ifQ.fmLGxsqBajOhJxycKNy8FiTC36sXa87Oo0GDmAZzzro";

    private async void FacturaDocNumero_LostFocus(object sender, RoutedEventArgs e)
    {
        string ruc = FacturaDocNumeroTextBox.Text.Trim();
        if (ruc.Length == 11)
        {
            FacturaRazonSocialTextBox.Text = "Buscando...";
            await ConsultarRucAsync(ruc, FacturaRazonSocialTextBox, FacturaDireccionTextBox);
        }
    }

    private async void BoletaDocNumero_LostFocus(object sender, RoutedEventArgs e)
    {
        string numero = BoletaDocNumeroTextBox.Text.Trim();
        string tipoDoc = ((ComboBoxItem)BoletaTipoDocComboBox.SelectedItem)?.Content.ToString();

        if (tipoDoc == "DNI" && numero.Length == 8)
        {
            BoletaNombreTextBox.Text = "Buscando...";
            await ConsultarDniAsync(numero, BoletaNombreTextBox);
        }
        else if (tipoDoc == "RUC" && numero.Length == 11)
        {
            BoletaNombreTextBox.Text = "Buscando...";
            await ConsultarRucAsync(numero, BoletaNombreTextBox, BoletaDireccionTextBox);
        }
    }

    private async Task ConsultarRucAsync(string ruc, TextBox razonSocialBox, TextBox direccionBox)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"https://dniruc.apisperu.com/api/v1/ruc/{ruc}?token={ApiToken}";
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        razonSocialBox.Text = GetJsonString(root, "razonSocial");
                        direccionBox.Text = GetJsonString(root, "direccion", "direccionCompleta");
                    }
                }
                else
                {
                    razonSocialBox.Text = "";
                    if (direccionBox != null) direccionBox.Text = "";
                }
            }
        }
        catch (Exception)
        {
            razonSocialBox.Text = "";
            if (direccionBox != null) direccionBox.Text = "";
        }
    }

    private async Task ConsultarDniAsync(string dni, TextBox nombreBox)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"https://dniruc.apisperu.com/api/v1/dni/{dni}?token={ApiToken}";
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        string nombres = GetJsonString(root, "nombres");
                        string apePat = GetJsonString(root, "apellidoPaterno");
                        string apeMat = GetJsonString(root, "apellidoMaterno");
                        
                        nombreBox.Text = $"{nombres} {apePat} {apeMat}".Trim();
                    }
                }
                else
                {
                    nombreBox.Text = "";
                }
            }
        }
        catch (Exception)
        {
            nombreBox.Text = "";
        }
    }

    private async Task<(bool EsValido, string Mensaje, string RazonSocial)> ValidarRucSunatAsync(string ruc)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"https://dniruc.apisperu.com/api/v1/ruc/{ruc}?token={ApiToken}";
                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return (false, "No se pudo validar el RUC en APISPERU. Intenta nuevamente.", string.Empty);
                }

                string json = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    string razonSocial = GetJsonString(root, "razonSocial");
                    string estado = GetJsonString(root, "estado", "estadoContribuyente").Trim();
                    string condicion = GetJsonString(root, "condicion", "condicionContribuyente").Trim();

                    bool esActivo = estado.Equals("ACTIVO", StringComparison.OrdinalIgnoreCase);
                    bool esHabido = condicion.Equals("HABIDO", StringComparison.OrdinalIgnoreCase);
                    bool tieneRazonSocial = !string.IsNullOrWhiteSpace(razonSocial);

                    if (!tieneRazonSocial || !esActivo || !esHabido)
                    {
                        return (false, "El RUC no es HABIDO o ACTIVO, o no cuenta con razón social válida.", razonSocial);
                    }

                    return (true, "RUC validado correctamente.", razonSocial);
                }
            }
        }
        catch (Exception)
        {
            return (false, "Ocurrió un error al consultar el RUC en APISPERU.", string.Empty);
        }
    }

    private static string GetJsonString(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.ValueKind == JsonValueKind.Null
                        ? string.Empty
                        : property.Value.GetString() ?? string.Empty;
                }
            }
        }

        return string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
