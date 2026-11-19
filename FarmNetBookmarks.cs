using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FarmNet;

public class FarmNetBookmarks
{
    private readonly string filePath;
    private readonly List<string> bookmarks = new();

    public IReadOnlyList<string> All =>
        bookmarks.AsReadOnly();

    public FarmNetBookmarks()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "FarmNet");

        Directory.CreateDirectory(folder);

        filePath = Path.Combine(
            folder,
            "browser-bookmarks.json");

        Load();
    }

    public bool Contains(string url)
    {
        return bookmarks.Exists(
            item => string.Equals(
                item,
                url,
                StringComparison.OrdinalIgnoreCase));
    }

    public bool Add(string url)
    {
        if (!IsValidUrl(url))
            return false;

        url = url.Trim();

        if (Contains(url))
            return false;

        bookmarks.Add(url);
        Save();

        return true;
    }

    public bool Remove(string url)
    {
        int index = bookmarks.FindIndex(
            item => string.Equals(
                item,
                url,
                StringComparison.OrdinalIgnoreCase));

        if (index < 0)
            return false;

        bookmarks.RemoveAt(index);
        Save();

        return true;
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(filePath))
                return;

            string json = File.ReadAllText(filePath);

            List<string>? loaded =
                JsonSerializer.Deserialize<List<string>>(json);

            if (loaded == null)
                return;

            foreach (string url in loaded)
            {
                if (IsValidUrl(url) && !Contains(url))
                    bookmarks.Add(url.Trim());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"FarmNet: Could not load bookmarks: {ex.Message}");
        }
    }

    private void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(
                bookmarks,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"FarmNet: Could not save bookmarks: {ex.Message}");
        }
    }

    private static bool IsValidUrl(string? url)
    {
        return !string.IsNullOrWhiteSpace(url) &&
            Uri.TryCreate(
                url.Trim(),
                UriKind.Absolute,
                out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp ||
             uri.Scheme == Uri.UriSchemeHttps);
    }
}
