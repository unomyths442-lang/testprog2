using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FieldViewer
{
    [Activity(Label = "Field Viewer", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private ListView listView;
        private ArrayAdapter<string> adapter;
        private List<string> displayItems = new List<string>();
        private AssemblyLoader loader = new AssemblyLoader();

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            try
            {
                SetContentView(Resource.Layout.activity_main);

                listView = FindViewById<ListView>(Resource.Id.listView);
                adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, displayItems);
                listView.Adapter = adapter;

                var openButton = FindViewById<Button>(Resource.Id.openButton);
                openButton.Click += OpenFilePicker;
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Ошибка инициализации: " + ex.Message, ToastLength.Long).Show();
            }
        }

        private void OpenFilePicker(object? sender, EventArgs e)
        {
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");
            StartActivityForResult(intent, 1);
        }

        protected override async void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == 1 && resultCode == Result.Ok && data?.Data != null)
            {
                try
                {
                    using var input = ContentResolver.OpenInputStream(data.Data);
                    string tempFile = Path.Combine(CacheDir.AbsolutePath, "assembly.dll");
                    using (var output = File.Create(tempFile))
                    {
                        await input.CopyToAsync(output);
                    }

                    await LoadAndDisplayFields(tempFile);
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "Ошибка: " + ex.Message, ToastLength.Long).Show();
                }
            }
        }

        private async Task LoadAndDisplayFields(string filePath)
        {
            displayItems.Clear();
            adapter.NotifyDataSetChanged();

            try
            {
                await loader.LoadAsync(filePath);

                var fields = await Task.Run(() => loader.GetAllFields());

                RunOnUiThread(() =>
                {
                    foreach (var field in fields)
                    {
                        displayItems.Add($"[{field.TypeName}] {field.FullDescription}");
                    }
                    adapter.NotifyDataSetChanged();
                    Toast.MakeText(this, $"Загружено полей: {fields.Count}", ToastLength.Short).Show();
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "Ошибка загрузки: " + ex.Message, ToastLength.Long).Show();
                });
            }
        }

        protected override void OnDestroy()
        {
            loader.Dispose();
            base.OnDestroy();
        }
    }
}
