using AwesomeAssertions;
using ListopiaParser.Configs;
using ListopiaParser.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;

namespace ListopiaParser.Tests.Services;

public class ListopiaServiceTests
{
    private IOptions<ListopiaOptions> _options;
    private ListopiaOptions _optionValues;
    private MockHttpMessageHandler _mockHttp;
    private Mock<ILogger<ListopiaService>> _loggerMock;
    private ListopiaService _sut;
    
    [SetUp]
    public void Setup()
    {
        _optionValues = new ListopiaOptions
        {
            GoodreadsBase = "https://www.goodreads.com",
            ListopiaUrl = "https://www.goodreads.com/list/show/001.TestList",
            SqsUrl = "https://sqs.us-east-1.amazonaws.com/123456/my-sqs",
            Pages = 10
        };
        _options = Options.Create(_optionValues);
        _mockHttp = new MockHttpMessageHandler();
        _loggerMock = new Mock<ILogger<ListopiaService>>();
        
        var client = new HttpClient(_mockHttp);
        _sut = new ListopiaService(client, _options, _loggerMock.Object);
    }

    [Test]
    public async Task TestGetListopiaIsbns()
    {
        var page = 1;
        var expectedIsbns = new List<string>
        {
            "1111111111111", "2222222222222", "3333333333333"
        };
        var listopiaRequest = _mockHttp.Expect(_optionValues.ListopiaUrl + "?page=1")
            .Respond("text/html", ListopiaResponse());
        var bookARequest = _mockHttp.Expect(_optionValues.GoodreadsBase + "/book/show/1-book-a")
            .Respond("text/html", BookResponse("Book A", 120, "1111111111111"));
        var bookBRequest = _mockHttp.Expect(_optionValues.GoodreadsBase + "/book/show/2-book-b")
            .Respond("text/html", BookResponse("Book B", 340, "2222222222222"));
        var bookCRequest = _mockHttp.Expect(_optionValues.GoodreadsBase + "/book/show/3-book-c")
            .Respond("text/html", BookResponse("Book C", 560, "3333333333333"));

        var isbnList = await _sut.GetListopiaIsbns(page, CancellationToken.None);
        
        Assert.That(_mockHttp.GetMatchCount(listopiaRequest), Is.EqualTo(1));
        Assert.That(_mockHttp.GetMatchCount(bookARequest), Is.EqualTo(1));
        Assert.That(_mockHttp.GetMatchCount(bookBRequest), Is.EqualTo(1));
        Assert.That(_mockHttp.GetMatchCount(bookCRequest), Is.EqualTo(1));
        Assert.That(isbnList, Is.Not.Null);
        Assert.That(isbnList.Count, Is.EqualTo(3));
        isbnList.Should().BeEquivalentTo(expectedIsbns);
    }
    
    [TearDown]
    public void TearDown()
    {
        _mockHttp.Dispose();
    }

    private static string ListopiaResponse()
    {
        return """
           <div id="all_votes">
               <table class="tableList js-dataTooltip">
           
                   <!-- Add query string params -->
           
                   <tr itemscope itemtype="http://schema.org/Book">
                       <td valign="top" class="number">1</td>
                       <td width="5%" valign="top">
                           <div id="120" class="u-anchorTarget"></div>
                           <div class="js-tooltipTrigger tooltipTrigger" data-resource-id="120" data-resource-type="Book">
                               <a title="Book A" href="/book/show/1-book-a">
                                   <img alt="Book A (Book A, #1)" class="bookCover" itemprop="image"
                                       src="https://i.gr-assets.com/images/S/compressed.photo.goodreads.com/books/some_name.jpg" />
                               </a>
                           </div>
                       </td>
                       <td width="100%" valign="top">
                           <a class="bookTitle" itemprop="url" href="/book/show/1-book-a">
                               <span itemprop='name' role='heading' aria-level='4'>Book A (Book A, #1)</span>
                           </a> <br />
                           <span class='by'>by</span>
                           <span itemprop='author' itemscope='' itemtype='http://schema.org/Person'>
                               <div class='authorName__container'>
                                   <a class="authorName" itemprop="url"
                                       href="https://www.goodreads.com/author/show/some_author"><span
                                           itemprop="name">John Doe</span></a>
                                   <span class="greyText">(Goodreads Author)</span>
                               </div>
                           </span>
           
                           <br />
                           <div>
                               <span class="greyText smallText uitext">
                                   <span class="minirating"><span class="stars staticStars notranslate"><span size="12x12"
                                               class="staticStar p10"></span><span size="12x12" class="staticStar p10"></span><span
                                               size="12x12" class="staticStar p10"></span><span size="12x12"
                                               class="staticStar p6"></span><span size="12x12" class="staticStar p0"></span></span>
                                       3.92 avg rating &mdash; 740,836 ratings</span>
                               </span>
                           </div>
           
           
                           <div style="margin-top: 5px">
                               <span class="smallText uitext">
                                   <a href="#"
                                       onclick=`Lightbox.showBoxByID(&#39;score_explanation&#39;, 300); return false;`>score:
                                       270,524</a>,
                                   <span class="greyText">and</span>
                                   <a id="loading_link_12" href="#"
                                       onclick=`onclick_stuff`>2,745
                                       people voted</a><img style="display:none" id="loading_anim_12" class="loading"
                                       src="https://s.gr-assets.com/assets/some_name.gif"
                                       alt="Loading trans" />
                                   &emsp;
           
                               </span>
                           </div>
                       </td>
                   </tr>
           
                   <!-- Add query string params -->
           
                   <tr itemscope itemtype="http://schema.org/Book">
                       <td valign="top" class="number">2</td>
                       <td width="5%" valign="top">
                           <div id="340" class="u-anchorTarget"></div>
                           <div class="js-tooltipTrigger tooltipTrigger" data-resource-id="340" data-resource-type="Book">
                               <a title="Book B" href="/book/show/2-book-b">
                                   <img alt="Book B (Book B, #1)" class="bookCover" itemprop="image"
                                       src="https://i.gr-assets.com/images/S/compressed.photo.goodreads.com/books/some_name.jpg" />
                               </a>
                           </div>
                       </td>
                       <td width="100%" valign="top">
                           <a class="bookTitle" itemprop="url" href="/book/show/2-book-b">
                               <span itemprop='name' role='heading' aria-level='4'>Book B (Book B, #1)</span>
                           </a> <br />
                           <span class='by'>by</span>
                           <span itemprop='author' itemscope='' itemtype='http://schema.org/Person'>
                               <div class='authorName__container'>
                                   <a class="authorName" itemprop="url"
                                       href="https://www.goodreads.com/author/show/some_author"><span
                                           itemprop="name">Jane Doe</span></a>
                                   <span class="greyText">(Goodreads Author)</span>
                               </div>
                           </span>
           
                           <br />
                           <div>
                               <span class="greyText smallText uitext">
                                   <span class="minirating"><span class="stars staticStars notranslate"><span size="12x12"
                                               class="staticStar p10"></span><span size="12x12" class="staticStar p10"></span><span
                                               size="12x12" class="staticStar p10"></span><span size="12x12"
                                               class="staticStar p6"></span><span size="12x12" class="staticStar p0"></span></span>
                                       3.72 avg rating &mdash; 615,870 ratings</span>
                               </span>
                           </div>
           
           
                           <div style="margin-top: 5px">
                               <span class="smallText uitext">
                                   <a href="#"
                                       onclick=`Lightbox.showBoxByID(&#39;score_explanation&#39;, 300); return false;`>score:
                                       222,367</a>,
                                   <span class="greyText">and</span>
                                   <a id="loading_link_34" href="#"
                                       onclick=`onclick_stuff`>2,266
                                       people voted</a><img style="display:none" id="loading_anim_34" class="loading"
                                       src="https://s.gr-assets.com/assets/some_name.gif"
                                       alt="Loading trans" />
                                   &emsp;
                               </span>
                           </div>
                       </td>
                   </tr>
           
                   <!-- Add query string params -->
           
                   <tr itemscope itemtype="http://schema.org/Book">
                       <td valign="top" class="number">3</td>
                       <td width="5%" valign="top">
                           <div id="560" class="u-anchorTarget"></div>
                           <div class="js-tooltipTrigger tooltipTrigger" data-resource-id="560" data-resource-type="Book">
                               <a title="Book C" href="/book/show/3-book-c">
                                   <img alt="Book C (Book C, #1)" class="bookCover" itemprop="image"
                                       src="https://i.gr-assets.com/images/S/compressed.photo.goodreads.com/books/some_name.jpg" />
                               </a>
                           </div>
                       </td>
                       <td width="100%" valign="top">
                           <a class="bookTitle" itemprop="url" href="/book/show/3-book-c">
                               <span itemprop='name' role='heading' aria-level='4'>Book C (Book C, #1)</span>
                           </a> <br />
                           <span class='by'>by</span>
                           <span itemprop='author' itemscope='' itemtype='http://schema.org/Person'>
                               <div class='authorName__container'>
                                   <a class="authorName" itemprop="url"
                                       href="https://www.goodreads.com/author/show/some_author"><span
                                           itemprop="name">Random Person</span></a>
                                   <span class="greyText">(Goodreads Author)</span>
                               </div>
                           </span>
           
                           <br />
                           <div>
                               <span class="greyText smallText uitext">
                                   <span class="minirating"><span class="stars staticStars notranslate"><span size="12x12"
                                               class="staticStar p10"></span><span size="12x12" class="staticStar p10"></span><span
                                               size="12x12" class="staticStar p10"></span><span size="12x12"
                                               class="staticStar p10"></span><span size="12x12"
                                               class="staticStar p3"></span></span>
                                       4.07 avg rating &mdash; 2,152,345 ratings</span>
                               </span>
                           </div>
           
           
                           <div style="margin-top: 5px">
                               <span class="smallText uitext">
                                   <a href="#"
                                       onclick=`Lightbox.showBoxByID(&#39;score_explanation&#39;, 300); return false;`>score:
                                       178,577</a>,
                                   <span class="greyText">and</span>
                                   <a id="loading_link_56" href="#"
                                       onclick=`onclick_stuff`>1,832
                                       people voted</a><img style="display:none" id="loading_anim_56" class="loading"
                                       src="https://s.gr-assets.com/assets/some_name.gif"
                                       alt="Loading trans" />
                                   &emsp;
                               </span>
                           </div>
                       </td>
                   </tr>
           
               </table>
           
           </div>
           """;
    }

    private static string BookResponse(string bookName, int bookId, string isbn13)
    {
        return $$"""
             <body>
                 
                 <script id="__NEXT_DATA__" type="application/json">
                     {
                         "props": {
                             "pageProps": {
                                 "apolloState": {
                                     "Series:kca://series/amzn1.gr.series.v1.some_name": {
                                         "__typename": "Series",
                                         "id": "kca://series/amzn1.gr.series.v1.some_name",
                                         "title": "{{bookName}}",
                                         "webUrl": "https://www.goodreads.com/series/some_url"
                                     },
                                     "Book:kca://book/amzn1.gr.book.v1.some_name": {
                                         "__typename": "Book",
                                         "id": "kca://book/amzn1.gr.book.v1.some_name",
                                         "legacyId": {{bookId}},
                                         "webUrl": "https://www.goodreads.com/book/show/some_url",
                                         "viewerShelving": null,
                                         "title": "{{bookName}}",
                                         "titleComplete": "{{bookName}} ({{bookName}}, #1)",
                                         "description": "Some cool and intriguing description.",
                                         "description({\"stripped\":true})": "Some cool and intriguing description.",
                                         "primaryContributorEdge": {
                                             "__typename": "BookContributorEdge",
                                             "node": {
                                                 "__ref": "Contributor:kca://author/amzn1.gr.author.v1.some_name"
                                             },
                                             "role": "Author"
                                         },
                                         "secondaryContributorEdges": [],
                                         "imageUrl": "https://m.media-amazon.com/images/S/compressed.photo.goodreads.com/books/some_name.jpg",
                                         "bookSeries": [
                                             {
                                                 "__typename": "BookSeries",
                                                 "userPosition": "1",
                                                 "series": {
                                                     "__ref": "Series:kca://series/amzn1.gr.series.v1.some_name"
                                                 }
                                             }
                                         ],
                                         "bookGenres": [
                                             {
                                                 "__typename": "BookGenre",
                                                 "genre": {
                                                     "__typename": "Genre",
                                                     "name": "Fantasy",
                                                     "webUrl": "https://www.goodreads.com/genres/fantasy"
                                                 }
                                             }
                                         ],
                                         "details": {
                                             "__typename": "BookDetails",
                                             "asin": null,
                                             "format": "Hardcover",
                                             "numPages": 391,
                                             "publicationTime": 1234567800000,
                                             "publisher": "Simon \u0026 Schuster",
                                             "isbn": null,
                                             "isbn13": "{{isbn13}}",
                                             "language": {
                                                 "__typename": "Language",
                                                 "name": "English"
                                             }
                                         },
                                         "reviewEditUrl": "https://www.goodreads.com/review/edit/{{bookId}}",
                                         "featureFlags": {
                                             "__typename": "FeatureFlags",
                                             "hideAds": false,
                                             "noIndex": false,
                                             "noReviews": false,
                                             "noNewRatings": false,
                                             "noNewTextReviews": false
                                         }
                                     }
                                 },
                                 "params": {
                                     "book_id": "{{bookId}}-some_url"
                                 },
                                 "query": {
                                     "book_id": "{{bookId}}-some_url"
                                 },
                                 "jwtToken": null,
                                 "dataSource": "Production",
                                 "authContextParams": {
                                     "signedIn": false,
                                     "customerId": null,
                                     "legacyCustomerId": null,
                                     "role": "user"
                                 },
                                 "userAgentContextParams": {
                                     "isWebView": null
                                 },
                                 "userAgent": "some_user_agent"
                             },
                             "__N_SSP": true
                         },
                         "page": "/book/show/[book_id]",
                         "query": {
                             "book_id": "{{bookId}}-some_url"
                         },
                         "buildId": "some_id",
                         "isFallback": false,
                         "isExperimentalCompile": false,
                         "gssp": true,
                         "locales": [
                             "en"  
                         ],
                         "scriptLoader": []
                     }
                 </script>

                 <div>
                     <!-- This is a random-length HTML comment: abc123@ -->
                 </div>
                 
             </body>

             """;
    }
}