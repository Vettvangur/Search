using Examine;
using Examine.LuceneEngine.Providers;
using Examine.LuceneEngine.Search;
using Examine.Search;
using Lucene.Net.QueryParsers;
using Search.Core.Models;
using Search.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Examine;
using Umbraco.Web;
using Umbraco.Core.Logging;
using Search.Core.Models.Enums;

namespace Search.Core.Services
{
    public class SearchService
    {
        private readonly ILogger _logger;
        private readonly IPublishedContentQuery _query;
        public SearchService(IPublishedContentQuery query, ILogger logger)
        {
            _logger = logger;
            _query = query;
        }
        public IEnumerable<PublishedSearchResult> Query(QueryRequest req, out long totalRecords)
        {
            totalRecords = 0;

            if (ExamineManager.Instance.TryGetIndex(req.Indexer, out var index) || !(index is IUmbracoIndex umbIndex))
            {

                var searcher = (BaseLuceneSearcher)index.GetSearcher();

                var luceneQuery = new StringBuilder();

                var queryWithOutStopWords = req.Query.RemoveStopWords();

                var cleanQuery = RemoveDiacritics(string.IsNullOrEmpty(queryWithOutStopWords) ? req.Query : queryWithOutStopWords);

                var searchTerms = cleanQuery
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(QueryParser.Escape);


                int i = 0;

                foreach (var term in searchTerms)
                {
                    if (i != 0)
                    {
                        luceneQuery.Append(" AND ");
                    }

                    if (i == 0)
                    {
                        luceneQuery.Append("+");
                    }

                    if (req.Fields == null || string.IsNullOrEmpty(queryWithOutStopWords))
                    {
                        luceneQuery.Append(" (");

                        if (req.SearchType == SearchType.Wildcard || req.SearchType == SearchType.FuzzyAndWilcard)
                        {
                            luceneQuery.Append("*" + term + "*");
                        }

                        if (req.SearchType == SearchType.Fuzzy || req.SearchType == SearchType.FuzzyAndWilcard)
                        {
                            luceneQuery.Append(" " + term + ("~" + req.FuzzyConfiguration));
                        }

                        luceneQuery.Append(")");

                    }
                    else
                    {
                        luceneQuery.Append(" (");
                        foreach (var field in req.Fields)
                        {
                            luceneQuery.Append(" (");
                            if (field.SearchType == SearchType.Wildcard || field.SearchType == SearchType.FuzzyAndWilcard)
                            {
                                luceneQuery.Append("(" + field.Name.FieldCultureName(req.Culture) + ": " + "*" + term + "*" + ")" + (!string.IsNullOrEmpty(field.Booster) ? field.Booster : ""));
                            }

                            if (field.SearchType == SearchType.Fuzzy || field.SearchType == SearchType.FuzzyAndWilcard)
                            {
                                luceneQuery.Append(" (" + field.Name.FieldCultureName(req.Culture) + ": " + term + "~" + field.FuzzyConfiguration + ")" + (!string.IsNullOrEmpty(field.Booster) ? field.Booster : ""));
                            }
                            luceneQuery.Append(")");
                        }
                        luceneQuery.Append(")");
                    }

                    i++;
                }

                IQuery searchQuery = searcher.CreateQuery("content");

                ((LuceneSearchQueryBase)searchQuery).QueryParser.AllowLeadingWildcard = true;

                var booleanOperation = searchQuery
                .NativeQuery(luceneQuery.ToString());


                if (req.NodeTypeAlias != null && req.NodeTypeAlias.Any())
                {
                    booleanOperation = booleanOperation.And().GroupedOr(new string[1] {
                        "__NodeTypeAlias"
                    }, req.NodeTypeAlias);
                }

                if (!string.IsNullOrEmpty(req.SearchNodeById))
                {
                    booleanOperation = booleanOperation.And().Field("searchPath", "|" + req.SearchNodeById + "|");
                }

                _logger.Debug<SearchService>(booleanOperation.ToString());

                var results = _query.Search(booleanOperation, req.Page - 1, req.PageSize, out totalRecords).OrderByDescending(x => x.Score);

                return results;

            }

            return null;
        }


        private string RemoveDiacritics(string text)
        {
            foreach (var characterMap in icelandicCharacters)
            {
                text = text.Replace(characterMap.Key, characterMap.Value);
            }

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        private Dictionary<string, string> icelandicCharacters = new Dictionary<string, string>
        {
            { "æ", "ae" },
            { "ð", "d" },
            { "þ", "th" },
            { "%", "" },
            { ";", "" },
            { "!", "" },
            { ",", "" },
            { "'", "" },
            { ".", "" },
            { "&", "" },
        };

        private Dictionary<string, string> stopWords = new Dictionary<string, string>
        {
            { "a", "" },
            { "an", "" },
            { "and", "" },
            { "are", "" },
            { "as", "" },
            { "at", "" },
            { "be", "" },
            { "but", "" },
            { "by", "" },
            { "for", "" },
            { "if", "" },
            { "in", "" },
            { "into", "" },
            { "is", "" },
            { "it", "" },
            { "no", "" },
            { "not", "" },
            { "of", "" },
            { "on", "" },
            { "or", "" },
            { "such", "" },
            { "that", "" },
            { "the", "" },
            { "their", "" },
            { "then", "" },
            { "there", "" },
            { "these", "" },
            { "they", "" },
            { "this", "" },
            { "to", "" },
            { "was", "" },
            { "will", "" },
            { "with", "" }
        };



    }
}
