using kindlewatcher;

string seriesToTrackFile = "SeriesToTrack.txt";
if (!File.Exists(seriesToTrackFile))
{
    Console.WriteLine($"{seriesToTrackFile} does not exist.");
    Console.WriteLine($"Please create it and try again.");
    Environment.Exit(0);
}

string[] seriesToTrackUrls = File.ReadAllLines(seriesToTrackFile);
if (seriesToTrackUrls.Length == 0)
{
    Console.WriteLine($"{seriesToTrackFile} does not contain any urls to track.");
    Console.WriteLine($"You can add URLs manually or by using the 'kindle add' command");
    Environment.Exit(0);
}

Console.WriteLine("Getting series info from the kindle store");
List<SeriesInfo> currentSeries = new List<SeriesInfo>();
for (int i = 0; i < seriesToTrackUrls.Length; i++)
{
    using(HttpClient client = new HttpClient())
    {
        HttpResponseMessage response = await client.GetAsync(seriesToTrackUrls[i]);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
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
        Console.WriteLine($"({i + 1}/{seriesToTrackUrls.Length}) {seriesInfo.Title}");
    }
}

string trackingDataFile = "TrackingData.txt";
if (!File.Exists(trackingDataFile))
{
    Console.WriteLine("Creating initial tracking data");

    string[] initialTrackingData = new string[currentSeries.Count];
    for (int i = 0; i < currentSeries.Count; i++)
    {
        initialTrackingData[i] = $"{currentSeries[i].Title}, {currentSeries[i].NumberOfBooks}, {currentSeries[i].KindleStoreUrl}";
        Console.WriteLine($"{currentSeries[i].Title} added to tracking data");
    }
    File.WriteAllLines(trackingDataFile, initialTrackingData);

    Console.WriteLine("Tracking data created");
    Console.WriteLine("Run the program again to check for changes in tracked series");
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
    trackingSeriesTitles.Add(row.Title);
}
List<SeriesInfo> newSeries = new List<SeriesInfo>();
foreach (var series in currentSeries)
{
    if (!trackingSeriesTitles.Contains(series.Title))
    {
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
        Console.WriteLine($"{newSeries[i].Title} {newSeries[i].NumberOfBooks} added to tracking data");
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

                int newBooksCount = trackingSeries[t].NumberOfBooks - currentSeries[c].NumberOfBooks;

                if (newBooksCount == 1)
                {
                    Console.WriteLine($"{trackingSeries[t].Title} has {newBooksCount} new book!");
                }
                else
                {
                    Console.WriteLine($"{trackingSeries[t].Title} has {newBooksCount} new books!");
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
    Console.WriteLine("There are no new books :(");
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
