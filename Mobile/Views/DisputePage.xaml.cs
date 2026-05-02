using FacePass.Mobile.Services;

namespace FacePass.Mobile.Views
{
    [QueryProperty(nameof(AttendanceId), "AttendanceId")]
    public partial class DisputePage : ContentPage
    {
        private readonly SupabaseMobileService _supabase;
        public string AttendanceId { get; set; } = string.Empty;

        public DisputePage(SupabaseMobileService supabase)
        {
            InitializeComponent();
            _supabase = supabase;
        }

        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DisputeReasonEditor.Text))
            {
                await DisplayAlert("Wait", "Please provide a reason for the dispute.", "OK");
                return;
            }

            try
            {
                if (Guid.TryParse(AttendanceId, out Guid id))
                {
                    await _supabase.SubmitDispute(id, DisputeReasonEditor.Text);
                    await DisplayAlert("Success", "Your dispute has been submitted for review.", "OK");
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to submit dispute. Try again later.", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
