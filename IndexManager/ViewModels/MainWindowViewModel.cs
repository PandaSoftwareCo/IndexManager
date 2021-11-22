using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Elasticsearch.Net;
using IndexManager.Framework;
using IndexManager.Interfaces;
using IndexManager.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using HttpMethod = System.Net.Http.HttpMethod;

namespace IndexManager.ViewModels
{
    public class MainWindowViewModel : BaseModel
    {
        private readonly HttpClient _httpClient;
        //private readonly IHttpService _httpService;
        private readonly ILogger<MainWindowViewModel> _logger;
        private IElasticLowLevelClient _lowClient;
        public IConfigurationRoot Configuration { get; }
        public AppSettings Options { get; set; }

        private IAppSettings _appSettings;
        private readonly IOptionsMonitor<AppSettings> _optionsMonitor;
        protected internal virtual IDisposable ChangeListener { get; }

        private string _url;
        [Required]
        [Url]
        public string Url
        {
            get
            {
                return _url;
            }
            set
            {
                if (_url != value)
                {
                    _url = value;

                    var node = new Uri(_url);
                    var settings = new ConnectionSettings(node);
                    settings.PrettyJson();
                    settings.MaximumRetries(10);
                    settings.RequestTimeout(TimeSpan.FromSeconds(120));
                    //var elasticClient = new ElasticClient(settings);
                    //_lowClient = elasticClient.LowLevel;
                    _lowClient = new ElasticLowLevelClient(settings);

                    OnPropertyChanged(nameof(Url));
                }
            }
        }

        private ObservableCollection<IndexModel> _indexes;
        public ObservableCollection<IndexModel> Indexes
        {
            get
            {
                return _indexes;
            }
            set
            {
                if (_indexes != value)
                {
                    _indexes = value;
                    OnPropertyChanged(nameof(Indexes));
                }
            }
        }

        private IndexModel _selectedIndex;
        public IndexModel SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    if (_selectedIndex != null && string.IsNullOrWhiteSpace(SelectedIndex.Definition))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, $"{Url}/{_selectedIndex.Name}?pretty");
                        var response = _httpClient.SendAsync(request).Result;
                        if (response.IsSuccessStatusCode)
                        {
                            SelectedIndex.Definition = response.Content.ReadAsStringAsync().Result;
                        }
                    }

                    OnPropertyChanged(nameof(SelectedIndex));
                }
            }
        }

        private ObservableCollection<AliasModel> _aliases;
        public ObservableCollection<AliasModel> Aliases
        {
            get
            {
                return _aliases;
            }
            set
            {
                if (_aliases != value)
                {
                    _aliases = value;
                    OnPropertyChanged(nameof(Aliases));
                }
            }
        }

        private AliasModel _selectedAlias;
        public AliasModel SelectedAlias
        {
            get
            {
                return _selectedAlias;
            }
            set
            {
                if (_selectedAlias != value)
                {
                    _selectedAlias = value;
                    OnPropertyChanged(nameof(SelectedAlias));
                }
            }
        }

        private ObservableCollection<TemplateModel> _templates;
        public ObservableCollection<TemplateModel> Templates
        {
            get
            {
                return _templates;
            }
            set
            {
                if (_templates != value)
                {
                    _templates = value;
                    OnPropertyChanged(nameof(Templates));
                }
            }
        }

        private TemplateModel _selectedTemplate;
        public TemplateModel SelectedTemplate
        {
            get
            {
                return _selectedTemplate;
            }
            set
            {
                if (_selectedTemplate != value)
                {
                    _selectedTemplate = value;
                    if (_selectedTemplate != null && string.IsNullOrWhiteSpace(_selectedTemplate.Definition))
                    {
                        Task.Run(() =>
                        {
                            var request1 = new HttpRequestMessage(HttpMethod.Get, $"{Url}/_template/{_selectedTemplate.Name}?pretty");
                            var response1 = _httpClient.SendAsync(request1).Result;
                            _selectedTemplate.Definition = response1.Content.ReadAsStringAsync().Result;
                        });
                    }

                    OnPropertyChanged(nameof(SelectedTemplate));
                }
            }
        }

        private string _indexName;
        public string IndexName
        {
            get
            {
                return _indexName;
            }
            set
            {
                if (_indexName != value)
                {
                    _indexName = value;
                    OnPropertyChanged(nameof(IndexName));
                }
            }
        }

        private string _aliasName;
        public string AliasName
        {
            get
            {
                return _aliasName;
            }
            set
            {
                if (_aliasName != value)
                {
                    _aliasName = value;
                    OnPropertyChanged(nameof(AliasName));
                }
            }
        }

        private string _selectedTemplateName;
        public string SelectedTemplateName
        {
            get
            {
                return _selectedTemplateName;
            }
            set
            {
                if (_selectedTemplateName != value)
                {
                    _selectedTemplateName = value;
                    OnPropertyChanged(nameof(SelectedTemplateName));
                }
            }
        }

        private string _searchName;
        public string SearchName
        {
            get
            {
                return _searchName;
            }
            set
            {
                if (_searchName != value)
                {
                    _searchName = value;
                    OnPropertyChanged(nameof(SearchName));
                }
            }
        }

        private string _searchRequest;
        public string SearchRequest
        {
            get
            {
                return _searchRequest;
            }
            set
            {
                if (_searchRequest != value)
                {
                    _searchRequest = value;
                    OnPropertyChanged(nameof(SearchRequest));
                }
            }
        }

        private string _response;
        public string Response
        {
            get
            {
                return _response;
            }
            set
            {
                if (_response != value)
                {
                    _response = value;
                    OnPropertyChanged(nameof(Response));
                }
            }
        }

        private DateTime _selectedDay;
        public DateTime SelectedDay
        {
            get
            {
                return _selectedDay;
            }
            set
            {
                if (_selectedDay != value)
                {
                    _selectedDay = value;

                    //https://orderbot.atlassian.net/browse/PROD-852
                    int year = _selectedDay.Year;
                    DateTime startDay = new DateTime(year, 1, 1);
                    int startDayOfWeek = (int) startDay.DayOfWeek - 1;
                    //WeekNumber = 1 + (_selectedDay.DayOfYear - 1) / 7;
                    WeekNumber = 1 + (_selectedDay.DayOfYear + startDayOfWeek - 1) / 7;
                    var format = $"{WeekNumber:D2}";

                    DayOfYear = _selectedDay.DayOfYear;

                    OnPropertyChanged(nameof(SelectedDay));
                }
            }
        }

        private int _dayOfYear;
        public int DayOfYear
        {
            get
            {
                return _dayOfYear;
            }
            set
            {
                if (_dayOfYear != value)
                {
                    _dayOfYear = value;
                    OnPropertyChanged(nameof(DayOfYear));
                }
            }
        }

        private int _weekNumber;
        public int WeekNumber
        {
            get
            {
                return _weekNumber;
            }
            set
            {
                if (_weekNumber != value)
                {
                    _weekNumber = value;
                    OnPropertyChanged(nameof(WeekNumber));
                }
            }
        }

        private int _size;
        public int Size
        {
            get
            {
                return _size;
            }
            set
            {
                if (_size != value)
                {
                    _size = value;
                    OnPropertyChanged(nameof(Size));
                }
            }
        }

        private int _count;
        public int Count
        {
            get
            {
                return _count;
            }
            set
            {
                if (_count != value)
                {
                    _count = value;
                    OnPropertyChanged(nameof(Count));
                }
            }
        }

        private string _sendRequest;
        public string SendRequest
        {
            get
            {
                return _sendRequest;
            }
            set
            {
                if (_sendRequest != value)
                {
                    _sendRequest = value;
                    OnPropertyChanged(nameof(SendRequest));
                }
            }
        }

        private string _status;
        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public MainWindowViewModel(HttpClient client, ILogger<MainWindowViewModel> logger, ILoggerFactory loggerFactory, IAppSettings appSettings, IConfigurationRoot config, IOptions<AppSettings> options, IOptionsMonitor<AppSettings> optionsMonitor)
        {
            _httpClient = client ?? throw new ArgumentNullException(nameof(client));
            //_httpService = httpService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            Configuration = config ?? throw new ArgumentNullException(nameof(config));
            Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            ChangeListener = _optionsMonitor.OnChange(OnMyOptionsChange);

            Url = _appSettings.Url;
            Indexes = new ObservableCollection<IndexModel>();
            Aliases = new ObservableCollection<AliasModel>();
            Templates = new ObservableCollection<TemplateModel>();
            SelectedTemplateName = "AwsConsumer";
            SearchName = "logs-awsconsumer-aliases";
            SearchRequest = File.ReadAllText(@$"Data\Search\Search.json");
            SendRequest = File.ReadAllText(@$"Data\Send\Send.json");
            SelectedDay = DateTime.Today;
            IndexName = _appSettings.IndexName;
            Size = _appSettings.Size;
            Count = _appSettings.Count;
            Status = "Ready";

            //var node = new Uri(Url);
            //var settings = new ConnectionSettings(node);
            //var elasticClient = new ElasticClient(settings);
            ////var lowClient = new ElasticLowLevelClient(settings);
            //_lowClient = elasticClient.LowLevel;
        }

        private void OnMyOptionsChange(AppSettings settings, string name)
        {
            _appSettings = settings;
            Options = settings;

            Url = _appSettings.Url;
            IndexName = _appSettings.IndexName;
            Size = _appSettings.Size;
            Count = _appSettings.Count;
        }

        private AsyncCommand _loadAsyncCommand;
        public AsyncCommand LoadAsyncCommand
        {
            get
            {
                if (_loadAsyncCommand == null)
                {
                    _loadAsyncCommand = new AsyncCommand(async execute => await LoadAsync(), canExecute => CanLoad());
                }
                return _loadAsyncCommand;
            }
            set
            {
                _loadAsyncCommand = value;
            }
        }

        public async Task LoadAsync()
        {
            var watchForParallel = Stopwatch.StartNew();
            Indexes.Clear();
            SelectedIndex = null;
            var url1 = $"{Url}/_cat/indices?format=json&pretty&s=index";
            var url2 = $"{Url}/_alias?pretty";
            Response = string.Empty;

            try
            {
                var request1 = new HttpRequestMessage(HttpMethod.Get, url1);
                var response1 = await _httpClient.SendAsync(request1);
                Response = await response1.Content.ReadAsStringAsync();
                Response += Environment.NewLine;
                _logger.LogInformation(Response);
                if (!response1.IsSuccessStatusCode)
                {
                    return;
                }
                var templates = JsonConvert.DeserializeObject<IndexModel[]>(Response);
                foreach (var template in templates)
                {
                    if (template.Name.StartsWith("."))
                    {
                        continue;
                    }
                    Indexes.Add(template);
                }

                var request = new HttpRequestMessage(HttpMethod.Get, $"{url2}");
                var response = await _httpClient.SendAsync(request);
                Response = await response.Content.ReadAsStringAsync();
                //_logger.LogInformation(Response);
                if (!response.IsSuccessStatusCode)
                {
                    return;
                }

                var doc = JsonDocument.Parse(Response);
                foreach (var index in doc.RootElement.EnumerateObject())
                {
                    if (index.Name.StartsWith("."))
                    {
                        continue;
                    }
                    foreach (var property in index.Value.EnumerateObject())
                    {
                        foreach (var alias in property.Value.EnumerateObject())
                        {
                            var model = Indexes.SingleOrDefault(i => i.Name == index.Name);
                            model.HasAlias = true;
                            model.Aliases.Add(new AliasModel { Name = alias.Name });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, Application.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);

                _logger.LogError(e.ToString());
            }

            watchForParallel.Stop();
            var time = watchForParallel.Elapsed;
            Status = $"GET: {url1} GET: {url2} ELAPSED: {time} COUNT: {Indexes.Count}";
            _logger.LogInformation(Status);
        }

        private bool CanLoad()
        {
            return true;
        }

        private AsyncCommand _updateIndexAsyncCommand;
        public AsyncCommand UpdateIndexAsyncCommand
        {
            get
            {
                if (_updateIndexAsyncCommand == null)
                {
                    _updateIndexAsyncCommand = new AsyncCommand(async execute => await UpdateIndexAsync(), canExecute => CanUpdateIndex());
                }
                return _updateIndexAsyncCommand;
            }
            set
            {
                _updateIndexAsyncCommand = value;
            }
        }

        public async Task UpdateIndexAsync()
        {
            var watchForParallel = Stopwatch.StartNew();

            try
            {
                //var url = $"{Url}/_template/template-{SelectedIndex.Name}-alias";
                //_logger.LogInformation(url);

                //var template0 = File.ReadAllText(@"Data\Template.json");
                //template0 = template0.Replace("INDEX_NAME", SelectedIndex.Name);
                //template0 = template0.Replace("ALIAS_NAME", SelectedIndex.Name.Replace("0000000001", "aliases"));
                //_logger.LogInformation(template0);

                //var request0 = new HttpRequestMessage(HttpMethod.Put, url);
                //var requestContent0 = new StringContent(template0, Encoding.UTF8, "application/json");
                //request0.Content = requestContent0;
                //var response0 = await _httpClient.SendAsync(request0);
                //Response = await response0.Content.ReadAsStringAsync();
                //_logger.LogInformation(Response);


                //var request1 = new HttpRequestMessage(HttpMethod.Delete, $"{Url}/{SelectedIndex.Name}");
                //var response1 = await _httpClient.SendAsync(request1);
                //Response = await response1.Content.ReadAsStringAsync();
                //_logger.LogInformation(Response);


                //var template = await File.ReadAllTextAsync(@"Data\Index\Index.json");
                //var requestContent = new StringContent(template, Encoding.UTF8, "application/json");
                //var request = new HttpRequestMessage(HttpMethod.Put, $"{Url}/my-logs-restapi-0000000001");
                //request.Content = requestContent;
                //var response = await _httpClient.SendAsync(request);
                //Response = await response.Content.ReadAsStringAsync();
                //_logger.LogInformation(Response);


                //EMPTY
                //var request = new HttpRequestMessage(HttpMethod.Put, $"{Url}/{IndexName.ToLower()}");
                //var response1 = await _httpClient.SendAsync(request);
                //Response = await response1.Content.ReadAsStringAsync();
                //_logger.LogInformation(Response);


                //var template = await File.ReadAllTextAsync(@"Data\Index\Update.json");
                //var requestContent = new StringContent(template, Encoding.UTF8, "application/json");
                //var request = new HttpRequestMessage(HttpMethod.Put, $"{Url}/logs-schedule-000004/_settings");
                //request.Content = requestContent;
                //var response = await _httpClient.SendAsync(request);
                //Response = await response.Content.ReadAsStringAsync();
                //_logger.LogInformation(Response);


                //var indexExistsResponse = await _lowClient.Indices.ExistsAsync<ExistsResponse>(IndexName.ToLower());
                //if (!indexExistsResponse.Exists)
                //{
                //    try
                //    {
                //        var response = await _lowClient.Indices.CreateAsync<DynamicResponse>(IndexName.ToLower(), null);
                //        _logger.LogInformation(response.DebugInformation);
                //        watchForParallel.Stop();
                //        var time = watchForParallel.Elapsed;
                //        Status = $"PUT: {response.Uri} {response.Success} ELAPSED: {time}";
                //        _logger.LogInformation(Status);
                //    }
                //    catch (Exception e)
                //    {
                //        _logger.LogError(e.Message, e);
                //        //Debug.WriteLine(e);
                //        throw;
                //    }

                //    //var aliasExistsResponse = _lowClient.Indices.Exists<ExistsResponse>(AliasName.ToLower());

                //    //if (!aliasExistsResponse.Exists)
                //    //{
                //    //    try
                //    //    {
                //    //        var response = _lowClient.Indices.PutAlias<DynamicResponse>(IndexName.ToLower(), AliasName.ToLower(), null);
                //    //        _logger.LogInformation(response.DebugInformation);
                //    //    }
                //    //    catch (Exception e)
                //    //    {
                //    //        _logger.LogError(e.Message, e);
                //    //        throw;
                //    //    }
                //    //}
                //}


                const string indexDetailsTemplate =
                    "{{ \"index\" : {{ \"_index\": \"{0}\", \"_type\": \"{1}\", \"_id\": \"{2}\" }} }}\n";
                var toInsertLogs = new StringBuilder();
                var numbers = Enumerable.Range(1, 5).ToList();
                foreach(var number in numbers)
                {
                    var logModel = new LogModel<object>
                    {
                        Timestamp = DateTime.Now,
                        Level = "Information",
                        MessageTemplate = "TEMPLATE",
                        RenderedMessage = $"MESSAGE {number}"
                    };
                    var log = JsonConvert.SerializeObject(logModel);
                    var type = "SCHEDULE";
                    var id = $"TEST-{number}-{Guid.NewGuid()}";

                    toInsertLogs.AppendFormat(indexDetailsTemplate, IndexName.ToLowerInvariant(), type, id);
                    toInsertLogs.Append($"{log}\n");
                }

                var response2 = await _lowClient.BulkAsync<DynamicResponse>(PostData.String(toInsertLogs.ToString()));
                _logger.LogInformation(response2.DebugInformation);
                Response = response2.DebugInformation;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }

        private bool CanUpdateIndex()
        {
            return !string.IsNullOrWhiteSpace(IndexName);
        }

        private AsyncCommand _deleteIndexAsyncCommand;
        public AsyncCommand DeleteIndexAsyncCommand
        {
            get
            {
                if (_deleteIndexAsyncCommand == null)
                {
                    _deleteIndexAsyncCommand = new AsyncCommand(async execute => await DeleteIndexAsync(), canExecute => CanDeleteIndex());
                }
                return _deleteIndexAsyncCommand;
            }
            set
            {
                _deleteIndexAsyncCommand = value;
            }
        }

        public async Task DeleteIndexAsync()
        {
            var watchForParallel = Stopwatch.StartNew();

            //int n = Indexes.Count(i => i.Selected);
            var selectedIndexes = Indexes.Where(i => i.Selected).ToArray();

            try
            {
                foreach (var selectedIndex in selectedIndexes)
                {
                    var request = new HttpRequestMessage(HttpMethod.Delete, $"{Url}/{selectedIndex.Name}");
                    var response = await _httpClient.SendAsync(request);
                    Response = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(Response);

                    if (response.IsSuccessStatusCode)
                    {
                        var ret = Indexes.Remove(selectedIndex);
                    }
                }
            }
            catch (Exception e)
            {
                Status = e.Message;
                _logger.LogError(e.ToString());
            }

            watchForParallel.Stop();
            var time = watchForParallel.Elapsed;
            Status = $"DELETE: {Url}/{string.Join(',', selectedIndexes.Select(i => i.Name).ToArray())} ELAPSED: {time}";
            _logger.LogInformation(Status);
        }

        private bool CanDeleteIndex()
        {
            //return SelectedIndex != null;
            return Indexes.Any(i => i.Selected);
        }

        private AsyncCommand _postAliasAsyncCommand;
        public AsyncCommand PostAliasAsyncCommand
        {
            get
            {
                if (_postAliasAsyncCommand == null)
                {
                    _postAliasAsyncCommand = new AsyncCommand(async execute => await PostAliasAsync(), canExecute => CanPostAlias());
                }
                return _postAliasAsyncCommand;
            }
            set
            {
                _postAliasAsyncCommand = value;
            }
        }

        public async Task PostAliasAsync()
        {
            var watchForParallel = Stopwatch.StartNew();

            try
            {
                var template = await File.ReadAllTextAsync(@"Data\Alias.json");
                template = template.Replace("INDEX_NAME", SelectedIndex.Name);
                //template = template.Replace("ALIAS_NAME", SelectedIndex.Name.Replace("0000000001", "aliases"));
                template = template.Replace("ALIAS_NAME", AliasName);
                _logger.LogInformation(template);

                var requestContent = new StringContent(template, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, $"{Url}/_aliases");
                request.Content = requestContent;
                var response = await _httpClient.SendAsync(request);
                Response = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(Response);

                SelectedIndex.HasAlias = true;
                //SelectedIndex.Aliases.Add(new AliasModel { Name = SelectedIndex.Name.Replace("0000000001", "aliases") });
                SelectedIndex.Aliases.Add(new AliasModel { Name = AliasName });
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            watchForParallel.Stop();
            var time = watchForParallel.Elapsed;
            Status = $"URL: {Url} ELAPSED: {time}";
            _logger.LogInformation(Status);
        }

        private bool CanPostAlias()
        {
            return SelectedIndex != null;
        }


        private AsyncCommand _loadAliasAsyncCommand;
        public AsyncCommand LoadAliasAsyncCommand
        {
            get
            {
                if (_loadAliasAsyncCommand == null)
                {
                    _loadAliasAsyncCommand = new AsyncCommand(async execute => await LoadAliasAsync(), canExecute => CanLoadAlias());
                }
                return _loadAliasAsyncCommand;
            }
            set
            {
                _loadAliasAsyncCommand = value;
            }
        }

        public async Task LoadAliasAsync()
        {
            var watchForParallel = Stopwatch.StartNew();

            Aliases.Clear();
            //var url1 = $"{Url}/_cat/indices?format=json&pretty";
            var url = $"{Url}/_alias?pretty";

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{url}");
                var response = await _httpClient.SendAsync(request);
                Response = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(Response);

                var doc = JsonDocument.Parse(Response);
                foreach (var index in doc.RootElement.EnumerateObject())
                {
                    if (index.Name.StartsWith("."))
                    {
                        continue;
                    }

                    foreach (var property in index.Value.EnumerateObject())
                    {
                        foreach (var alias in property.Value.EnumerateObject())
                        {
                            if(!Aliases.Any(i => i.Name == alias.Name))
                            {
                                var model = new AliasModel { Name = alias.Name };
                                Aliases.Add(model);
                            }
                        }
                    }
                }

                var tasks = new List<Task<HttpResponseMessage>>();
                foreach (var alias in Aliases)
                {
                    var requestAlias = new HttpRequestMessage(HttpMethod.Get, $"{Url}/{alias.Name}/_count?pretty");
                    var task = _httpClient.SendAsync(requestAlias);
                    tasks.Add(task);
                }
                await Task.WhenAll(tasks);

                foreach(var task in tasks)
                {
                    var requestUrl = task.Result.RequestMessage.RequestUri.AbsolutePath.TrimStart('/').Replace(@"/_count", string.Empty);
                    var responseString = await task.Result.Content.ReadAsStringAsync();
                    var alias = Aliases.SingleOrDefault(i => i.Name == requestUrl);
                    var count = JsonConvert.DeserializeObject<IndexManager.Models.CountResponse>(responseString);
                    alias.Count = count.Count;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }


            watchForParallel.Stop();
            var time = watchForParallel.Elapsed;
            Status = $"GET: {url} ELAPSED: {time}";
            _logger.LogInformation(Status);
        }

        private bool CanLoadAlias()
        {
            return true;
        }

        private AsyncCommand _loadTemplatesAsyncCommand;
        public AsyncCommand LoadTemplatesAsyncCommand
        {
            get
            {
                if (_loadTemplatesAsyncCommand == null)
                {
                    _loadTemplatesAsyncCommand = new AsyncCommand(async execute => await LoadTemplatesAsync(), canExecute => CanLoadTemplates());
                }
                return _loadTemplatesAsyncCommand;
            }
            set
            {
                _loadTemplatesAsyncCommand = value;
            }
        }

        public async Task LoadTemplatesAsync()
        {
            var watchForParallel = Stopwatch.StartNew();

            Templates.Clear();

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{Url}/_cat/templates?format=json&pretty");
                var response = await _httpClient.SendAsync(request);
                Response = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(Response);
                var templates = JsonConvert.DeserializeObject<TemplateModel[]>(Response);
                foreach (var template in templates)
                {
                    if (template.Name.StartsWith("."))
                    {
                        continue;
                    }

                    Templates.Add(template);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            watchForParallel.Stop();
            var time = watchForParallel.Elapsed;
            Status = $"GET: {Url}/_cat/templates?format=json&pretty ELAPSED: {time}";
            _logger.LogInformation(Status);
        }

        private bool CanLoadTemplates()
        {
            return true;
        }

        private AsyncCommand _postTemplateAsyncCommand;
        public AsyncCommand PostTemplateAsyncCommand
        {
            get
            {
                if (_postTemplateAsyncCommand == null)
                {
                    _postTemplateAsyncCommand = new AsyncCommand(async execute => await PostTemplateAsync(), canExecute => CanPostTemplate());
                }
                return _postTemplateAsyncCommand;
            }
            set
            {
                _postTemplateAsyncCommand = value;
            }
        }

        public async Task PostTemplateAsync()
        {
            var watchForParallel = Stopwatch.StartNew();

            try
            {
                //var request3 = new HttpRequestMessage(HttpMethod.Put, $"{Url}/_index_template/test-index-template");
                //var requestContent3 = new StringContent(File.ReadAllText(@"Data\IndexTemplate.json"), Encoding.UTF8, "application/json");
                //request3.Content = requestContent3;
                //var response3 = await _httpClient.SendAsync(request3);
                //var content3 = await response3.Content.ReadAsStringAsync();
                //_logger.LogInformation(content3);


                //var url = $"{Url}/_template/template-{SelectedIndex.Name}-alias";
                //_logger.LogInformation(url);

                //var template = File.ReadAllText(@"Data\Template.json");
                //template = template.Replace("INDEX_NAME", SelectedIndex.Name);
                //template = template.Replace("ALIAS_NAME", SelectedIndex.Name.Replace("0000000001", "aliases"));
                //_logger.LogInformation(template);

                //var request = new HttpRequestMessage(HttpMethod.Put, url);
                //var requestContent = new StringContent(template, Encoding.UTF8, "application/json");
                //request.Content = requestContent;
                //var response = await _httpClient.SendAsync(request);
                //Response = await response.Content.ReadAsStringAsync();
                //_logger.LogInformation(Response);

                var request = new HttpRequestMessage(HttpMethod.Put, $"{Url}/_template/{SelectedTemplateName.ToLower()}-template");
                var requestContent = new StringContent(File.ReadAllText(@$"Data\TemplateAlias\{SelectedTemplateName}.json"), Encoding.UTF8, "application/json");
                request.Content = requestContent;
                var response = await _httpClient.SendAsync(request);
                Response = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(Response);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            watchForParallel.Stop();
            var time = watchForParallel.Elapsed;
            Status = $"URL: {Url} ELAPSED: {time}";
            _logger.LogInformation(Status);
        }

        private bool CanPostTemplate()
        {
            return true;
            //return SelectedIndex != null && SelectedIndex.Name.StartsWith("logs-") && SelectedIndex.Name.EndsWith("-0000000001");
        }


        private AsyncCommand _deleteTemplateAsyncCommand;
        public AsyncCommand DeleteTemplateAsyncCommand
        {
            get
            {
                if (_deleteTemplateAsyncCommand == null)
                {
                    _deleteTemplateAsyncCommand = new AsyncCommand(async execute => await DeleteTemplateAsync(), canExecute => CanDeleteTemplate());
                }
                return _deleteTemplateAsyncCommand;
            }
            set
            {
                _deleteTemplateAsyncCommand = value;
            }
        }

        public async Task DeleteTemplateAsync()
        {
            var watchForParallel = Stopwatch.StartNew();

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"{Url}/_template/{SelectedTemplate.Name}");
                //var request = new HttpRequestMessage(HttpMethod.Delete, $"{Url}/_component_template/{SelectedTemplate.Name}");
                var response = await _httpClient.SendAsync(request);
                Response = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(Response);
                if (response.IsSuccessStatusCode)
                {
                    Templates.Remove(SelectedTemplate);
                    SelectedTemplate = null;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            watchForParallel.Stop();
            var time = watchForParallel.Elapsed;
            Status = $"URL: {Url} ELAPSED: {time}";
            _logger.LogInformation(Status);
        }

        private bool CanDeleteTemplate()
        {
            return SelectedTemplate != null;
        }

        private AsyncCommand _searchAsyncCommand;
        public AsyncCommand SearchAsyncCommand
        {
            get
            {
                if (_searchAsyncCommand == null)
                {
                    _searchAsyncCommand = new AsyncCommand(async execute => await SearchAsync(), canExecute => CanSearch());
                }
                return _searchAsyncCommand;
            }
            set
            {
                _searchAsyncCommand = value;
            }
        }

        public async Task SearchAsync()
        {
            var watchForParallel = Stopwatch.StartNew();
            //var url = $"{Url}/{SearchName}/_search?pretty";
            var url = $"{Url}/{SelectedIndex.Name}/_search?pretty";

            try
            {
                //var search = File.ReadAllText(@$"Data\Search\Search.json");

                //var request = new HttpRequestMessage(HttpMethod.Get, $"{Url}/alias-logs/_search?pretty");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var requestContent = new StringContent(SearchRequest, Encoding.UTF8, "application/json");
                request.Content = requestContent;
                var response = await _httpClient.SendAsync(request);
                Response = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(Response);
            }
            catch (Exception e)
            {
                Response = e.ToString();
                _logger.LogError(e.ToString());
            }

            watchForParallel.Stop();
            var time = watchForParallel.Elapsed;
            Status = $"GET: {url} ELAPSED: {time}";
            _logger.LogInformation(Status);
        }

        private bool CanSearch()
        {
            return !string.IsNullOrWhiteSpace(SearchName);
        }

        private AsyncCommand _sendAsyncCommand;
        public AsyncCommand SendAsyncCommand
        {
            get
            {
                if (_sendAsyncCommand == null)
                {
                    _sendAsyncCommand = new AsyncCommand(async execute => await SendAsync(), canExecute => CanSend());
                }
                return _sendAsyncCommand;
            }
            set
            {
                _sendAsyncCommand = value;
            }
        }

        public async Task SendAsync()
        {
            var watchForParallel = Stopwatch.StartNew();
            //var url = $"{Url}/{SearchName}/_search?pretty";
            //var url = $"{Url}/{SelectedIndex.Name}/_search?pretty";

            try
            {
                const string indexDetailsTemplate =
                    "{{ \"index\" : {{ \"_index\": \"{0}\", \"_type\": \"{1}\", \"_id\": \"{2}\" }} }}\n";

                //Parallel.For(1, 10_000, (i) =>
                //{
                //    Debug.WriteLine($"SEND {i}");
                //});
                var numbers = Enumerable.Range(1, Count);
                var requests = new List<HttpRequestMessage>();
                var requestStrings = new List<string>();
                foreach (var number in numbers)
                {
                    //Debug.WriteLine($"SEND {number}");

                    var toInsertLogs = new StringBuilder();
                    var numbers1 = Enumerable.Range(1, Size).ToList();
                    foreach (var j in numbers1)
                    {
                        var logModel = new LogModel<object>
                        {
                            Timestamp = DateTime.Now,
                            Level = "Information",
                            MessageTemplate = "TEMPLATE",
                            RenderedMessage = $"MESSAGE {j}"
                        };
                        var log = JsonConvert.SerializeObject(logModel);
                        var type = "SCHEDULE";
                        var id = $"TEST-{number}-{j}-{Guid.NewGuid()}";

                        toInsertLogs.AppendFormat(indexDetailsTemplate, IndexName, type, id);
                        toInsertLogs.AppendLine($"{log}");
                    }

                    var request = new HttpRequestMessage(HttpMethod.Post, $"{Url}/_bulk");
                    request.Content = new StringContent(toInsertLogs.ToString(), Encoding.UTF8, "application/json");
                    requests.Add(request);
                    requestStrings.Add(toInsertLogs.ToString());
                }

                var tasks = new List<Task<HttpResponseMessage>>();
                foreach (var request in requests)
                {
                    var responseTask = _httpClient.SendAsync(request);
                    tasks.Add(responseTask);
                }

                await Task.WhenAll(tasks);
                foreach (var task in tasks)
                {
                    Response = await task.Result.Content.ReadAsStringAsync();
                    //_logger.LogInformation(Response);
                    //var count = JsonConvert.DeserializeObject<IndexManager.Models.CountResponse>(responseString);
                    if (!task.Result.IsSuccessStatusCode || task.Result.StatusCode != HttpStatusCode.OK)
                    {
                        _logger.LogInformation(Response);
                    }
                }

                //var dynamicResponseTasks = new List<Task<DynamicResponse>>();
                //foreach (var requestString in requestStrings)
                //{
                //    //var people = new object[]
                //    //{
                //    //    new { index = new { _index = "people", _type = "person", _id = "1"  }},
                //    //    new { FirstName = "Martijn", LastName = "Laarman" },
                //    //    new { index = new { _index = "people", _type = "person", _id = "2"  }},
                //    //    new { FirstName = "Greg", LastName = "Marzouka" },
                //    //    new { index = new { _index = "people", _type = "person", _id = "3"  }},
                //    //    new { FirstName = "Russ", LastName = "Cam" },
                //    //};
                //    //var ndexResponse = lowlevelClient.Bulk<StringResponse>(PostData.MultiJson(people));
                //    //var response2 = await _lowClient.BulkAsync<DynamicResponse>(PostData.String(request.Content?.ReadAsStringAsync().Result));
                //    var dynamicResponseTask = _lowClient.BulkAsync<DynamicResponse>(PostData.String(requestString), new BulkRequestParameters
                //    {
                //        Timeout = TimeSpan.FromSeconds(100),
                //        RequestConfiguration = new RequestConfiguration
                //        {
                //            DisableSniff = true,
                //            MaxRetries = 10,
                //            RequestTimeout = TimeSpan.FromSeconds(100)
                //        }
                //    });
                //    dynamicResponseTasks.Add(dynamicResponseTask);
                //}
                //await Task.WhenAll(dynamicResponseTasks);
                //foreach (var dynamicResponseTask in dynamicResponseTasks)
                //{
                //    var dynamicResponse = dynamicResponseTask.Result;
                //    HttpStatusCode code = (HttpStatusCode)dynamicResponse.HttpStatusCode;
                //    var ret = dynamicResponse.DebugInformation;
                //    string responseStream = dynamicResponse.Body;
                //}
            }
            catch (Exception e)
            {
                //while (e.InnerException != null)
                //{
                //    e = e.InnerException;
                //}
                Response = e.ToString();
                _logger.LogError(e.ToString());
            }

            watchForParallel.Stop();
            var time = watchForParallel.Elapsed;
            Status = $"SEND REST: ELAPSED: {time}";
            _logger.LogInformation(Status);
        }

        private bool CanSend()
        {
            return !string.IsNullOrWhiteSpace(SearchName);
        }

        private AsyncCommand _sendNetAsyncCommand;
        public AsyncCommand SendNetAsyncCommand
        {
            get
            {
                if (_sendNetAsyncCommand == null)
                {
                    _sendNetAsyncCommand = new AsyncCommand(async execute => await SendNetAsync(), canExecute => CanSendNet());
                }
                return _sendNetAsyncCommand;
            }
            set
            {
                _sendNetAsyncCommand = value;
            }
        }

        public async Task SendNetAsync()
        {
            //var url = $"{Url}/{SearchName}/_search?pretty";
            //var url = $"{Url}/{SelectedIndex.Name}/_search?pretty";

            try
            {
                const string indexDetailsTemplate =
                    "{{ \"index\" : {{ \"_index\": \"{0}\", \"_type\": \"{1}\", \"_id\": \"{2}\" }} }}\n";

                //Parallel.For(1, 10_000, (i) =>
                //{
                //    Debug.WriteLine($"SEND {i}");
                //});
                var numbers = Enumerable.Range(1, Count);
                var requestStrings = new List<string>();
                foreach (var number in numbers)
                {
                    //Debug.WriteLine($"SEND {number}");

                    var toInsertLogs = new StringBuilder();
                    var numbers1 = Enumerable.Range(1, Size).ToList();
                    foreach (var j in numbers1)
                    {
                        var logModel = new LogModel<object>
                        {
                            Timestamp = DateTime.UtcNow,
                            Level = "Information",
                            MessageTemplate = "TEMPLATE",
                            RenderedMessage = $"MESSAGE {j}"
                        };
                        var log = JsonConvert.SerializeObject(logModel, new JsonSerializerSettings
                        {
                            //DateFormatHandling = DateFormatHandling.IsoDateFormat
                            //DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
                            DateFormatString = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'"
                        });
                        var type = "SCHEDULE";
                        var id = $"TEST-{number}-{j}-{Guid.NewGuid()}";

                        toInsertLogs.AppendFormat(indexDetailsTemplate, IndexName, type, id);
                        toInsertLogs.AppendLine($"{log}");
                    }

                    requestStrings.Add(toInsertLogs.ToString());
                }

                var watchForParallel = Stopwatch.StartNew();
                var dynamicResponseTasks = new List<Task<DynamicResponse>>();
                foreach (var requestString in requestStrings)
                {
                    //var people = new object[]
                    //{
                    //    new { index = new { _index = "people", _type = "person", _id = "1"  }},
                    //    new { FirstName = "Martijn", LastName = "Laarman" },
                    //    new { index = new { _index = "people", _type = "person", _id = "2"  }},
                    //    new { FirstName = "Greg", LastName = "Marzouka" },
                    //    new { index = new { _index = "people", _type = "person", _id = "3"  }},
                    //    new { FirstName = "Russ", LastName = "Cam" },
                    //};
                    //var ndexResponse = lowlevelClient.Bulk<StringResponse>(PostData.MultiJson(people));
                    //var response2 = await _lowClient.BulkAsync<DynamicResponse>(PostData.String(request.Content?.ReadAsStringAsync().Result));
                    //var dynamicResponseTask = _lowClient.BulkAsync<DynamicResponse>(PostData.String(requestString), new BulkRequestParameters
                    //{
                    //    RequestConfiguration = new RequestConfiguration
                    //    {
                    //        //DisableSniff = true,
                    //        MaxRetries = 10,
                    //        RequestTimeout = TimeSpan.FromSeconds(120)
                    //    }
                    //});
                    var dynamicResponseTask = _lowClient.BulkAsync<DynamicResponse>(PostData.String(requestString));
                    dynamicResponseTasks.Add(dynamicResponseTask);
                }
                await Task.WhenAll(dynamicResponseTasks);
                watchForParallel.Stop();
                var time = watchForParallel.Elapsed;
                Status = $"SEND ElasticSearch.NET: Count: {Count} Size: {Size} ELAPSED: {time}";

                foreach (var dynamicResponseTask in dynamicResponseTasks)
                {
                    var dynamicResponse = dynamicResponseTask.Result;
                    var code = (HttpStatusCode)dynamicResponse.HttpStatusCode;
                    var ret = dynamicResponse.DebugInformation;
                    string responseStream = dynamicResponse.Body.ToString();
                    if (!dynamicResponse.Success)
                    {

                    }
                }
            }
            catch (Exception e)
            {
                Response = e.ToString();
                _logger.LogError(e.ToString());
            }

            _logger.LogInformation(Status);
        }

        private bool CanSendNet()
        {
            return !string.IsNullOrWhiteSpace(SearchName);
        }

        private AsyncCommand _sendRequestAsyncCommand;
        public AsyncCommand SendRequestAsyncCommand
        {
            get
            {
                if (_sendRequestAsyncCommand == null)
                {
                    _sendRequestAsyncCommand = new AsyncCommand(async execute => await SendRequestAsync(), canExecute => CanSendRequest());
                }
                return _sendRequestAsyncCommand;
            }
            set
            {
                _sendRequestAsyncCommand = value;
            }
        }

        public async Task SendRequestAsync()
        {
            var watchForParallel = Stopwatch.StartNew();

            try
            {
                var dynamicResponse = await _lowClient.BulkAsync<DynamicResponse>(PostData.String(SendRequest), new BulkRequestParameters
                {
                    RequestConfiguration = new RequestConfiguration
                    {
                        //DisableSniff = true,
                        MaxRetries = 10,
                        RequestTimeout = TimeSpan.FromSeconds(100)
                    }
                });
                var status = dynamicResponse.HttpStatusCode;
                var success = dynamicResponse.Success;
                _logger.LogInformation(dynamicResponse.DebugInformation);



                var request = new HttpRequestMessage(HttpMethod.Post, $"{Url}/_bulk");
                request.Content = new StringContent(SendRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                Response = await response.Content.ReadAsStringAsync();
                //_logger.LogInformation(Response);
                //var count = JsonConvert.DeserializeObject<IndexManager.Models.CountResponse>(responseString);
                if (!response.IsSuccessStatusCode || response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogInformation(Response);
                }
            }
            catch (Exception e)
            {
                //while (e.InnerException != null)
                //{
                //    e = e.InnerException;
                //}
                Response = e.ToString();
                _logger.LogError(e.ToString());
            }

            watchForParallel.Stop();
            var time = watchForParallel.Elapsed;
            Status = $"SEND REST: ELAPSED: {time}";
            _logger.LogInformation(Status);
        }

        private bool CanSendRequest()
        {
            return !string.IsNullOrWhiteSpace(SearchName);
        }
    }
}
