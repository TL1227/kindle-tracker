﻿using static System.Console;
using kindlewatcher;

string seriesToTrackFile = "SeriesToTrack.txt";
if (!File.Exists(seriesToTrackFile))
{
    File.Create(seriesToTrackFile);
    WriteLine($"Created {seriesToTrackFile}");
    WriteLine($"Add the urls of any kindle series you want to track");
    WriteLine($"For example, to track DragonBall add the following");
    WriteLine($"https://www.amazon.co.jp/DRAGON-BALL-%E3%83%A2%E3%83%8E%E3%82%AF%E3%83%AD%E7%89%88-1-%E3%82%B8%E3%83%A3%E3%83%B3%E3%83%97%E3%82%B3%E3%83%9F%E3%83%83%E3%82%AF%E3%82%B9DIGITAL-ebook/dp/B00A47VS5A");
    WriteLine($"Run the program again when you've added some urls");
    Environment.Exit(0);
}

string[] seriesToTrackUrls = File.ReadAllLines(seriesToTrackFile);

if (seriesToTrackUrls.Length == 0)
{
    WriteLine($"{seriesToTrackFile} does not contain any urls to track.");
    Environment.Exit(0);
}

WriteLine("Getting series info from the kindle store");
List<SeriesInfo> currentSeries = new List<SeriesInfo>();
for (int i = 0; i < seriesToTrackUrls.Length; i++)
{
    using(HttpClient client = new HttpClient())
    {
        HttpResponseMessage response = client.GetAsync(seriesToTrackUrls[i]).Result;
        response.EnsureSuccessStatusCode();
        string responseBody = response.Content.ReadAsStringAsync().Result;
        response.Dispose();

        string[] bookTotalSplit = responseBody.Split("第 1 巻");
        string bookTotalAndTitle = bookTotalSplit[1].Split("</a>")[0];
        string title = bookTotalAndTitle.Split(":")[1].Trim();
        int numberOfBooks = int.Parse(bookTotalAndTitle.Split(" ")[2]);

        SeriesInfo seriesInfo = new SeriesInfo()
        {
            Title = title,
            NumberOfBooks = numberOfBooks,
            KindleStoreUrl = seriesToTrackUrls[i]
        };

        currentSeries.Add(seriesInfo);
        WriteLine($"({i + 1}/{seriesToTrackUrls.Length}) {seriesInfo.Title}");
    }
}

string trackingDataFile = "TrackingData.txt";
if (!File.Exists(trackingDataFile))
{
    WriteLine("Creating initial tracking data");
    WriteLine("Tracking data created");

    string[] initialTrackingData = new string[currentSeries.Count];
    for (int i = 0; i < currentSeries.Count; i++)
    {
        initialTrackingData[i] = $"{currentSeries[i].Title}, {currentSeries[i].NumberOfBooks}, {currentSeries[i].KindleStoreUrl}";
        WriteLine($"{currentSeries[i].Title} added to tracking data");
    }
    File.WriteAllLines(trackingDataFile, initialTrackingData);

    WriteLine("Run the program again to check for changes in tracked series");
    Environment.Exit(0);
}

List<SeriesInfo> trackingSeries = new List<SeriesInfo>();
string[] trackingFileRows = File.ReadAllLines(trackingDataFile);
for (int i = 0; i < trackingFileRows.Length; i++)
{
    string[] seriesInfo = trackingFileRows[i].Split(",");

    SeriesInfo series = new SeriesInfo()
    {
        Title = seriesInfo[0],
        NumberOfBooks = int.Parse(seriesInfo[1]),
        KindleStoreUrl = seriesInfo[2],
        NewTitles = false
    };

    trackingSeries.Add(series);
}

//TODO: Creating the newSeries list should also be more elegant than this
//check if there are any new series in the seriesToTrack file
List<string> trackingSeriesTitles =  new List<string>();
foreach (var row in trackingSeries)
{
    if (row.Title != null)
        trackingSeriesTitles.Add(row.Title);
}
List<SeriesInfo> newSeries = new List<SeriesInfo>();
foreach (var series in currentSeries)
{
    if (series.Title != null)
    {
        if (!trackingSeriesTitles.Contains(series.Title))
            newSeries.Add(series);
    }
}

//add any new series to the tracking data  
if (newSeries.Count != 0)
{
    string[] newSeriesToAdd = new string[newSeries.Count];
    for (int i = 0; i < newSeries.Count; i++)
    {
        newSeriesToAdd[i] = $"{newSeries[i].Title}, {newSeries[i].NumberOfBooks}, {newSeries[i].KindleStoreUrl}";
        WriteLine($"{newSeries[i].Title} {newSeries[i].NumberOfBooks} added to tracking data");
    }
    File.AppendAllLines(trackingDataFile, newSeriesToAdd);
}

//TODO: refactor this because it's ugly and I'm sure I can do better!
//compare the new book totals with the previous book totals 
for (int t = 0; t < trackingSeries.Count; t++)
{
    for (int c = 0;  c < currentSeries.Count;  c++)
    {
        if (trackingSeries[t].Title == currentSeries[c].Title)
        {
            if (trackingSeries[t].NumberOfBooks < currentSeries[c].NumberOfBooks)
            {
                currentSeries[c].NewTitles = true;

                int newBooksCount = currentSeries[c].NumberOfBooks - trackingSeries[t].NumberOfBooks; 

                if (newBooksCount == 1)
                {
                    WriteLine($"{trackingSeries[t].Title} has {newBooksCount} new book!");
                }
                else
                {
                    WriteLine($"{trackingSeries[t].Title} has {newBooksCount} new books!");
                }
            }
        }
    }
}

//check for new titles 
bool newTitles = false;
for (int i = 0; i < currentSeries.Count; i++)
{
    if (currentSeries[i].NewTitles == true)
    {
        newTitles = true;
    }
}

if (!newTitles)
{
    WriteLine("There are no new books :(");
    Environment.Exit(0);
}

//TODO: ask the user if they want to update their tracking data before doing it 
string[] currentSeriesToAdd = ConvertSeriesInfoListToCsvRowArray(currentSeries);
File.WriteAllLines(trackingDataFile, currentSeriesToAdd);


string[] ConvertSeriesInfoListToCsvRowArray(List<SeriesInfo> seriesInfos)
{
    string[] csvRows = new string[seriesInfos.Count];
    for (int i = 0; i < csvRows.Length; i++)
    {
        csvRows[i] = $"{seriesInfos[i].Title},{seriesInfos[i].NumberOfBooks},{seriesInfos[i].KindleStoreUrl}";
    }

    return csvRows;
}
