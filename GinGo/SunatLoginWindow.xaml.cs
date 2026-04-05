using System.Windows;

namespace GinGo;

public partial class SunatLoginWindow : Window
{
    private string _appUsername;

    public SunatLoginWindow(string appUsername)
    {
        InitializeComponent();
        _appUsername = appUsername;
    }

    private void ValidateSunat_Click(object sender, RoutedEventArgs e)
    {
        var ruc = SunatRucTextBox.Text.Trim();
        var userSol = SunatUserTextBox.Text.Trim();
        var passSol = SunatPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(ruc) || string.IsNullOrWhiteSpace(userSol) || string.IsNullOrWhiteSpace(passSol))
        {
            MessageBox.Show("Por favor, completa el RUC, Usuario y Contraseña SOL.", "Validación SUNAT", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Bypass temporal para facilitar el acceso a pruebas sin escribir los 11 dígitos reales y demás validaciones
        // Credenciales por defecto para pasar al Dashboard:
        // RUC: 10452369871
        // Usuario: ADMINSOL
        // Pass: Sol123!
        if (ruc == "10452369871" && userSol == "ADMINSOL" && passSol == "Sol123!")
        {
            var dashboardAdmin = new DashboardWindow(ruc, _appUsername);
            dashboardAdmin.Show();
            this.Close();
            return;
        }

        if (ruc.Length != 11 || !long.TryParse(ruc, out _))
        {
            MessageBox.Show("El RUC debe contener exactamente 11 números.", "Validación SUNAT", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Aquí en el futuro conectarías con la API de Greented / SUNAT
        // Por ahora simulamos que todo está OK y pasamos al Dashboard
        
        MessageBox.Show("Credenciales SUNAT validadas correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

        var dashboard = new DashboardWindow(ruc, _appUsername);
        dashboard.Show();
        this.Close();
    }

    private void BackToLogin_Click(object sender, RoutedEventArgs e)
    {
        var login = new MainWindow();
        login.Show();
        this.Close();
    }
}