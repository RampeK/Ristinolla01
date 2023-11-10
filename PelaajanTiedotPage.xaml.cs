using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Ristinolla01
{
    public partial class PelaajanTiedotPage : ContentPage
    {
        // Pelaajan etunimen m��rittely
        public string PelaajanEtunimi { get; private set; }


        public PelaajanTiedotPage()
        {
            InitializeComponent();
            // Asetetaan tapahtumank�sittelij�t painikkeelle ja valitsimelle
            Startbutton.Clicked += OnStartGameButtonClicked;
            ExistingPlayersPicker.SelectedIndexChanged += OnExistingPlayerSelected;
        }

        // T�t� metodia kutsutaan, kun sivu ilmestyy n�yt�lle
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Ladataan pelaajien tiedot
            var pelaajat = PelaajaTiedotManager.LataaPelaajat();
            foreach (var pelaaja in pelaajat)
            {
                ExistingPlayersPicker.Items.Add($"{pelaaja.Etunimi} {pelaaja.Sukunimi}");
            }

            // Tietokoneen tilastojen p�ivitys
            var tietokonePelaaja = pelaajat.FirstOrDefault(p => p.Etunimi == "Tietokone");
            if (tietokonePelaaja != null)
            {
                ComputerVoitotLabel.Text = $"Tietokoneen voitot: {tietokonePelaaja.Voitot}";
                ComputerTappiotLabel.Text = $"Tietokoneen tappiot: {tietokonePelaaja.Tappiot}";
                ComputerTasapelitLabel.Text = $"Tietokoneen tasapelit: {tietokonePelaaja.Tasapelit}";
            }
        }


        private async void OnStartGameButtonClicked(Object sender, EventArgs e)
        {
            // Lue sy�tetyt tiedot
            string etunimi = FirstNameEntry.Text;
            string sukunimi = LastNameEntry.Text;
            string syntymavuosiStr = BirthYearEntry.Text;

            // K�ytt�j�n sy�tteen validointi
            if (string.IsNullOrEmpty(etunimi) || string.IsNullOrEmpty(sukunimi))
            {
                await DisplayAlert("Virhe", "Etunimi ja sukunimi ovat pakollisia tietoja.", "OK");
                return;
            }

            if (!int.TryParse(syntymavuosiStr, out int syntymavuosi) || syntymavuosi < 1900 || syntymavuosi > DateTime.Now.Year)
            {
                await DisplayAlert("Virhe", "Sy�t� kelvollinen syntym�vuosi.", "OK");
                return;
            }

            var pelaajat = PelaajaTiedotManager.LataaPelaajat();
            var olemassaOlevaPelaaja = pelaajat.Find(p => p.Etunimi == etunimi && p.Sukunimi == sukunimi);

            MainPage mainGamePage;

            if (olemassaOlevaPelaaja == null)
            {
                var uusiPelaaja = new Pelaaja
                {
                    Etunimi = etunimi,
                    Sukunimi = sukunimi,
                    SyntymaVuosi = syntymavuosi
                };

                pelaajat.Add(uusiPelaaja);
                mainGamePage = new MainPage();
                MainPage.AsetaPelaajanNimi(uusiPelaaja.Etunimi);
            }
            else
            {
                olemassaOlevaPelaaja.SyntymaVuosi = syntymavuosi;
                mainGamePage = new MainPage();
                MainPage.AsetaPelaajanNimi(olemassaOlevaPelaaja.Etunimi);
            }

            // Tarkista pelimuoto ja l�het� se MainPage:lle.
            if (PlayerVsPlayer.IsChecked == true)
            {
                MainPage.SetGameMode(true);  // Pelaaja vs Pelaaja
                MainPage.SetIsPlayerVsAI(false);
            }
            else
            {
                MainPage.SetGameMode(false); // Pelaaja vs AI
                MainPage.SetIsPlayerVsAI(true);
            }

            bool tallennettu = PelaajaTiedotManager.SavePlayers(pelaajat);
            if (!tallennettu)
            {
                await DisplayAlert("Virhe", "Pelaajan tietojen tallennus ep�onnistui.", "OK");
                return;
            }
            // Navigointi peliin
            await Shell.Current.GoToAsync("//mainGame");
        }


        // Tapahtumank�sittelij� "Peruuta"-painikkeelle
        void OnCancelClicked(object sender, EventArgs e)
        {
            Application.Current.Quit();
        }

        // Tapahtumank�sittelij�, kun valitaan olemassa oleva pelaaja
        private void OnExistingPlayerSelected(object sender, EventArgs e)
        {
            var selectedPlayerName = ExistingPlayersPicker.SelectedItem.ToString();
            var names = selectedPlayerName.Split(' ');
            if (names.Length > 1)
            {
                var selectedPlayer = PelaajaTiedotManager.LataaPelaajat().Find(p => p.Etunimi == names[0] && p.Sukunimi == names[1]);
                if (selectedPlayer != null)
                {
                    FirstNameEntry.Text = selectedPlayer.Etunimi;
                    LastNameEntry.Text = selectedPlayer.Sukunimi;
                    BirthYearEntry.Text = selectedPlayer.SyntymaVuosi.ToString();

                    // P�ivit� tilastot
                    VoitotLabel.Text = $"Voitot: {selectedPlayer.Voitot}";
                    TappiotLabel.Text = $"Tappiot: {selectedPlayer.Tappiot}";
                    TasapelitLabel.Text = $"Tasapelit: {selectedPlayer.Tasapelit}";
                }
            }
        }
    }

    // Pelaaja-luokan m��rittely
    public class Pelaaja
    {
        // Ominaisuudet pelaajalle
        public string Etunimi { get; set; }
        public string Sukunimi { get; set; }
        public int SyntymaVuosi { get; set; }
        public int Voitot { get; set; }
        public int Tappiot { get; set; }
        public int Tasapelit { get; set; }
        public TimeSpan keskiarvo { get; set; }
    }


    // Apuluokka pelaajatietojen tallentamiseen ja lataamiseen
    public class PelaajaTiedotManager
    {
        // M��ritell��n tiedostonimi, johon tiedot tallennetaan
        private static readonly string TiedostoNimi = GetLocalFilePath("pelaajatiedot.json");

        // Tallenna pelaajien tiedot tiedostoon
        public static bool SavePlayers(List<Pelaaja> pelaajat)
        {
            try
            {
                var json = JsonSerializer.Serialize(pelaajat);
                File.WriteAllText(TiedostoNimi, json);
                Debug.WriteLine($"Pelaajat tallennettu onnistuneesti tiedostoon {TiedostoNimi}");
                return true; // Onnistunut tallennus
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Virhe tallennettaessa pelaajia: {ex.Message}");
                return false; // Ep�onnistunut tallennus
            }
        }

        // Lataa pelaajien tiedot tiedostosta
        public static List<Pelaaja> LataaPelaajat()
        {
            try
            {
                if (File.Exists(TiedostoNimi))
                {
                    var json = File.ReadAllText(TiedostoNimi);
                    return JsonSerializer.Deserialize<List<Pelaaja>>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Virhe ladattaessa pelaajia: {ex.Message}");
            }
            return new List<Pelaaja>();
        }

        // Apufunktio saamaan paikallisen tiedostopolun
        private static string GetLocalFilePath(string filename)
        {
            string projectDirectory = System.AppDomain.CurrentDomain.BaseDirectory;                      
            return Path.Combine(projectDirectory, filename);           
        }
    }
}
