using System;
using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;

namespace SafariBooksDownload
{
    public class LazyLoadBehavior : Behavior<Image>
    {
        public static readonly BindableProperty SourceProperty =
            BindableProperty.Create(nameof(Source), typeof(string), typeof(LazyLoadBehavior));

        public string Source
        {
            get => (string)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private Image _associatedImage;

        protected override void OnAttachedTo(Image bindable)
        {
            base.OnAttachedTo(bindable);
            _associatedImage = bindable;
            bindable.PropertyChanged += OnImagePropertyChanged;
        }

        protected override void OnDetachingFrom(Image bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.PropertyChanged -= OnImagePropertyChanged;
            _associatedImage = null;
        }

        private async void OnImagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == VisualElement.IsVisibleProperty.PropertyName && _associatedImage.IsVisible)
            {
                // Load image asynchronously when the Image becomes visible
                _associatedImage.Source = ImageSource.FromUri(new Uri(Source));
            }
        }
    }
}
