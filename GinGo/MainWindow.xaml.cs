using System.Windows;
using GinGo.Data;

namespace GinGo;

public partial class MainWindow : Window
{
    private readonly UserRepository _userRepository = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var identifier = LoginUserOrEmailTextBox.Text.Trim();
        var password = LoginPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Completa usuario/correo y contraseña.", "GinGo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            LoginButton.IsEnabled = false;

            // Bypass para visualización rápida (admin / Admin123!)
            if (identifier == "admin" && password == "Admin123!")
            {
                var dashboard = new DashboardWindow("", identifier);
                dashboard.Show();
                this.Close();
                return;
            }

            var isValid = await _userRepository.ValidateCredentialsAsync(identifier, password);
            if (!isValid)
            {
                MessageBox.Show("Credenciales inválidas.", "GinGo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dashboardWindow = new DashboardWindow("", identifier);
            dashboardWindow.Show();
            this.Close();
        }
        catch (Exception ex)
        {
            // Si hay error de DB pero las credenciales son admin, forzamos la entrada para que veas el diseño
            if (identifier == "admin" && password == "Admin123!")
            {
                var dashboard = new DashboardWindow("", identifier);
                dashboard.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show($"No se pudo iniciar sesión.\n{ex.Message}", "GinGo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void CreateAccountLink_Click(object sender, RoutedEventArgs e)
    {
        var window = new RegisterWindow();
        window.Owner = this;
        window.ShowDialog();
    }
}
