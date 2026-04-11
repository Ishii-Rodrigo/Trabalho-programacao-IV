using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using umfg.venda.app.Abstracts;
using umfg.venda.app.ViewModels;

namespace umfg.venda.app.Commands
{
    internal sealed class FinalizarRecebimentoCommand : AbstractCommand
    {
        public override void Execute(object? parameter)
        {
            var viewModel = parameter as ReceberPedidoViewModel;

            if (viewModel == null)
            {
                MessageBox.Show("Erro interno ao processar pagamento.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<string> erros = new List<string>();

            if (viewModel.TipoCartaoSelecionado <= 0)
                erros.Add("- Selecione o tipo de cartão.");

            viewModel.NomeCartao = viewModel.NomeCartao?.ToUpper().Trim();

            if (string.IsNullOrWhiteSpace(viewModel.NomeCartao))
            {
                erros.Add("- Nome no cartão é obrigatório.");
            }
            else if (!Regex.IsMatch(viewModel.NomeCartao, @"^[A-ZÀ-Ÿ]{2,}(?:\s[A-ZÀ-Ÿ]{2,})+$"))
            {
                erros.Add("- Informe o nome completo (apenas letras, mínimo de duas palavras).");
            }

            if (string.IsNullOrWhiteSpace(viewModel.NumeroCartao))
            {
                erros.Add("- Número do cartão é obrigatório.");
            }
            else if (!ValidarCartaoLuhn(viewModel.NumeroCartao))
            {
                erros.Add("- Número do cartão inválido.");
            }

            if (string.IsNullOrWhiteSpace(viewModel.CVV) || !Regex.IsMatch(viewModel.CVV, @"^\d{3}$"))
            {
                erros.Add("- CVV deve conter exatamente 3 dígitos numéricos.");
            }

            var dataValidade = viewModel.ObterDataValidade();

            if (dataValidade == null)
            {
                erros.Add("- Data de validade deve estar no formato MM/yyyy.");
            }
            else
            {
                var ultimoDiaCartao = new DateTime(
                    dataValidade.Value.Year,
                    dataValidade.Value.Month,
                    DateTime.DaysInMonth(dataValidade.Value.Year, dataValidade.Value.Month)
                );

                if (ultimoDiaCartao < DateTime.Today)
                    erros.Add("- Cartão vencido.");
            }

            if (erros.Any())
            {
                MessageBox.Show(
                    "Pagamento recusado:\n\n" + string.Join("\n", erros),
                    "Erro de Validação",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(
                "Pagamento realizado com sucesso!",
                "Sucesso",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.DataContext = new MainWindowViewModel();
            }
        }

        private bool ValidarCartaoLuhn(string numero)
        {
            string digits = Regex.Replace(numero, @"\D", "");

            if (digits.Length < 13 || digits.Length > 19)
                return false;

            int sum = 0;
            bool alternate = false;

            for (int i = digits.Length - 1; i >= 0; i--)
            {
                int n = int.Parse(digits[i].ToString());

                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }

                sum += n;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }
    }
}