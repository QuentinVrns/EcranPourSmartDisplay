using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;
using System.IO;

namespace EcranPourSmartDisplay
{
    public partial class MainWindow : Window
    {
        private string selectedSalle;
        private bool isImageLoopRunning = false;
        public MainWindow()
        {
            InitializeComponent();
            LoadSalles();
        }

        public class Salle
        {
            public int Id { get; set; }
            public string NomSalle { get; set; }
            public int EtageId { get; set; }
        }

        private async void LoadSalles()
        {
            string url = "https://quentinvrns.alwaysdata.net/getAllClasse";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();



                    List<Salle> salles = JsonConvert.DeserializeObject<List<Salle>>(responseBody);

                    if (salles == null || salles.Count == 0)
                    {
                        MessageBox.Show("Aucune salle disponible.");
                        return;
                    }

                    salleComboBox.Items.Clear();
                    foreach (var salle in salles)
                    {
                        salleComboBox.Items.Add(salle.NomSalle);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Erreur lors de la récupération des salles : {e.Message}");
                }
            }
        }

        private void salleComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            selectedSalle = salleComboBox.SelectedItem as string;
            // Afficher la salle sélectionnée pour débogage
            // MessageBox.Show($"Salle sélectionnée : {selectedSalle}"); // Optionnel pour débogage
        }

        private async void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedSalle))
            {
                MessageBox.Show("Veuillez sélectionner une salle.");
                return;
            }

            // Afficher la salle sélectionnée pour débogage
            // MessageBox.Show($"Salle sélectionnée : {selectedSalle}"); // Optionnel pour débogage

            // Cacher le logo, la ComboBox et le bouton Valider, afficher le bouton de retour
            logoImage.Visibility = Visibility.Collapsed;
            salleComboBox.Visibility = Visibility.Collapsed;
            validateButton.Visibility = Visibility.Collapsed;
            TitreProject.Visibility = Visibility.Collapsed;
            backButton.Visibility = Visibility.Visible;

            // Charger les images pour la salle sélectionnée
            await LoadImages(selectedSalle);
        }

        private async Task LoadImages(string salleName)
        {
            string baseImageUrl = "https://quentinvrns.fr/Document/";
            string salleUrl = $"{baseImageUrl}{salleName}/";
            List<int> availableImages = new List<int>();
            int imageIndex = 1;

            // Première passe pour identifier les images disponibles
            while (true)
            {
                string imageUrl = $"{salleUrl}image{imageIndex}.png";
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        HttpResponseMessage response = await client.GetAsync(imageUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            availableImages.Add(imageIndex);
                        }
                        else
                        {
                            if (availableImages.Count == 0)
                            {
                                // Afficher un message si aucune image n'est disponible
                                MessageBox.Show("Aucune image disponible pour la salle sélectionnée.");
                                ResetToHomePage(); // Réinitialiser à la page d'accueil
                                return;
                            }
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la vérification de l'image {imageIndex}: {ex.Message}");
                        ResetToHomePage(); // Réinitialiser à la page d'accueil
                        return;
                    }
                }
                imageIndex++;
            }

            if (availableImages.Count == 0)
            {
                MessageBox.Show("Aucune image disponible pour la salle sélectionnée.");
                ResetToHomePage(); // Réinitialiser à la page d'accueil
                return;
            }

            // Démarrer la boucle d'affichage des images
            isImageLoopRunning = true;
            await DisplayImagesLoop(availableImages, salleUrl);
        }

        private async Task DisplayImagesLoop(List<int> availableImages, string salleUrl)
        {
            while (isImageLoopRunning)
            {
                foreach (var index in availableImages)
                {
                    string imageUrl = $"{salleUrl}image{index}.png";
                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            HttpResponseMessage response = await client.GetAsync(imageUrl);
                            if (response.IsSuccessStatusCode)
                            {
                                byte[] imageData = await response.Content.ReadAsByteArrayAsync();
                                BitmapImage bitmap = new BitmapImage();
                                using (MemoryStream ms = new MemoryStream(imageData))
                                {
                                    ms.Seek(0, SeekOrigin.Begin);
                                    bitmap.BeginInit();
                                    bitmap.StreamSource = ms;
                                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmap.EndInit();
                                }
                                imageControl.Source = bitmap;
                                imageControl.Visibility = Visibility.Visible;
                            }
                            await Task.Delay(5000); // Attendre 5 secondes avant de passer à la prochaine image
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la récupération de l'image {index}: {ex.Message}");
                        ResetToHomePage(); // Réinitialiser à la page d'accueil en cas d'erreur
                        return;
                    }
                }
            }
        }

        private void ResetToHomePage()
        {
            // Arrêter la boucle d'affichage des images
            isImageLoopRunning = false;

            // Réinitialiser l'affichage
            imageControl.Visibility = Visibility.Collapsed;
            backButton.Visibility = Visibility.Collapsed;
            logoImage.Visibility = Visibility.Visible;
            salleComboBox.Visibility = Visibility.Visible;
            validateButton.Visibility = Visibility.Visible;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ResetToHomePage(); // Réinitialiser à la page d'accueil
        }
    }
}