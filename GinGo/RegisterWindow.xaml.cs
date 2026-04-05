using System.Net.Mail;
using System.Windows;
using System.Windows.Media;
using GinGo.Data;

namespace GinGo;

public partial class RegisterWindow : Window
{
    private readonly UserRepository _userRepository = new();

    private int _strengthLevel;

    public RegisterWindow()
    {
        InitializeComponent();
        UpdateStrengthUi();
        UpdateRegisterButtonState();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AnyField_Changed(object sender, RoutedEventArgs e)
    {
        UpdateRegisterButtonState();
    }

    private void PasswordBoxes_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _strengthLevel = CalculatePasswordStrengthLevel(PasswordBox.Password);
        UpdateStrengthUi();
        UpdateRegisterButtonState();
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameTextBox.Text.Trim();
        var email = EmailTextBox.Text.Trim();
        var phone = PhoneTextBox.Text.Trim();
        var password = PasswordBox.Password;
        var repeatPassword = RepeatPasswordBox.Password;

        if (!IsValidEmail(email))
        {
            MessageBox.Show("Correo electrónico inválido.", "GinGo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (password != repeatPassword)
        {
            MessageBox.Show("Las contraseñas no coinciden.", "GinGo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_strengthLevel != 2)
        {
            MessageBox.Show("La contraseña debe ser fuerte (barra verde) para registrar.", "GinGo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            RegisterButton.IsEnabled = false;
            var result = await _userRepository.CreateUserAsync(username, email, string.IsNullOrWhiteSpace(phone) ? null : phone, password);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "GinGo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Usuario registrado correctamente.", "GinGo", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo registrar.\n{ex.Message}", "GinGo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            UpdateRegisterButtonState();
        }
    }

    private void UpdateRegisterButtonState()
    {
        var usernameOk = !string.IsNullOrWhiteSpace(UsernameTextBox.Text);
        var emailOk = IsValidEmail(EmailTextBox.Text.Trim());
        var passwordOk = _strengthLevel == 2;
        var matchOk = PasswordBox.Password == RepeatPasswordBox.Password && !string.IsNullOrWhiteSpace(PasswordBox.Password);

        RegisterButton.IsEnabled = usernameOk && emailOk && passwordOk && matchOk;
    }

    private void UpdateStrengthUi()
    {
        var inactive = new SolidColorBrush(Color.FromRgb(226, 232, 240));
        var red = new SolidColorBrush(Color.FromRgb(239, 68, 68));
        var yellow = new SolidColorBrush(Color.FromRgb(245, 158, 11));
        var green = new SolidColorBrush(Color.FromRgb(22, 163, 74));

        StrengthRect1.Fill = inactive;
        StrengthRect2.Fill = inactive;
        StrengthRect3.Fill = inactive;
        StrengthLabel.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));

        if (_strengthLevel <= 0)
        {
            StrengthRect1.Fill = red;
            StrengthLabel.Text = "Débil";
            StrengthLabel.Foreground = red;
            return;
        }

        if (_strengthLevel == 1)
        {
            StrengthRect1.Fill = red;
            StrengthRect2.Fill = yellow;
            StrengthLabel.Text = "Media";
            StrengthLabel.Foreground = yellow;
            return;
        }

        StrengthRect1.Fill = red;
        StrengthRect2.Fill = yellow;
        StrengthRect3.Fill = green;
        StrengthLabel.Text = "Fuerte";
        StrengthLabel.Foreground = green;
    }

    private static int CalculatePasswordStrengthLevel(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return 0;
        }

        var lengthOk = password.Length >= 8;
        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        var categories = 0;
        if (hasLetter) categories++;
        if (hasDigit) categories++;
        if (hasSpecial) categories++;

        if (!lengthOk || categories <= 1)
        {
            return 0;
        }

        if (categories == 2)
        {
            return 1;
        }

        return 2;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
