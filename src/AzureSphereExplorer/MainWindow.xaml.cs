﻿using AzureSpherePublicAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AzureSphereExplorer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private AzureSphereAPI _Api = new AzureSphereAPI();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await _Api.AuthenticationAsync(cancellationTokenSource.Token);
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to authenticate.", null, MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            var tenants = await _Api.GetTenantsAsync(cancellationTokenSource.Token);
            var dialog = new TenantsWindow();
            dialog.Owner = this;
            dialog.Tenants = tenants;
            var dialogResult = dialog.ShowDialog();
            if (!dialogResult.Value)
            {
                Close();
                return;
            }
            var tenant = dialog.SelectedTenant;
            dialog = null;

            this.Title = $"Azure Sphere Explorer - {tenant.Name}";

            var products = await _Api.GetProductsAsync(tenant, cancellationTokenSource.Token);
            var deviceGroups = await _Api.GetDeviceGroupsAsync(tenant, cancellationTokenSource.Token);
            var devices = await _Api.GetDevicesAsync(tenant, cancellationTokenSource.Token);

            this.gridProducts.ItemsSource = from v in products
                                            select new { 
                                                Product = v.Name,
                                                v.Description
                                            };

            this.gridDeviceGroups.ItemsSource = from v in deviceGroups
                                                join p in products on v.ProductId equals p.Id
                                                select new {
                                                    Product = p.Name,
                                                    DeviceGroup = v.Name,
                                                    v.Description,
                                                    OsFeedType = v.OsFeedTypeStr,
                                                    UpdatePolicy = v.UpdatePolicyStr,
                                                    CurrentDeploymentDateUtc = v.CurrentDeployment?.DeploymentDateUtc
                                                };

            this.gridDevices.ItemsSource = from v in devices
                                           join dg in deviceGroups on v.DeviceGroupId equals dg.Id into gj
                                           from dg_ in gj.DefaultIfEmpty()
                                           join p in products on dg_?.ProductId equals p.Id into gj2
                                           from p_ in gj2.DefaultIfEmpty()
                                           select new { 
                                               Product = p_?.Name,
                                               DeviceGroup = dg_?.Name,
                                               ChipSku = v.ChipSkuStr,
                                               DeviceId = v.Id
                                           };
        }

        private void menuitemDeviceGroupDeployments_Click(object sender, RoutedEventArgs e)
        {
            //var deviceGroup = gridDeviceGroups.SelectedItem as AzureSphereDeviceGroup;
        }
    }
}
