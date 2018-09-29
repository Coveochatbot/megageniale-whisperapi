﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using WhisperAPI.Models;
using WhisperAPI.Models.NLPAPI;
using WhisperAPI.Models.Queries;
using WhisperAPI.Models.Search;
using WhisperAPI.Services.MLAPI.Facets;
using WhisperAPI.Services.NLPAPI;
using WhisperAPI.Services.Search;
using WhisperAPI.Services.Suggestions;
using WhisperAPI.Tests.Data.Builders;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class SuggestionsServiceTest
    {
        private ISuggestionsService _suggestionsService;

        private Mock<IIndexSearch> _indexSearchMock;
        private Mock<INlpCall> _nlpCallMock;
        private Mock<IDocumentFacets> _documentFacetsMock;
        private ConversationContext _conversationContext;

        [SetUp]
        public void SetUp()
        {
            this._indexSearchMock = new Mock<IIndexSearch>();
            this._nlpCallMock = new Mock<INlpCall>();
            this._documentFacetsMock = new Mock<IDocumentFacets>();

            this._suggestionsService = new SuggestionsService(this._indexSearchMock.Object, this._nlpCallMock.Object, this._documentFacetsMock.Object, this.GetIrrelevantIntents());
            this._conversationContext = new ConversationContext(new Guid("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d9"), DateTime.Now);
        }

        [Test]
        [TestCase]
        public void When_receive_valid_searchresult_from_search_then_return_list_of_suggestedDocuments()
        {
            var intents = new List<Intent>
            {
                IntentBuilder.Build.WithName("Need Help").Instance
            };

            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intents).Instance;

            this._indexSearchMock
                .Setup(x => x.Search(It.IsAny<string>()))
                .Returns(this.GetSearchResult());

            this._nlpCallMock
                .Setup(x => x.GetNlpAnalysis(It.IsAny<string>()))
                .Returns(nlpAnalysis);

            this._suggestionsService.GetSuggestedDocuments(this.GetConversationContext()).Should().BeEquivalentTo(this.GetSuggestedDocuments());
        }

        [Test]
        [TestCase]
        public void When_receive_empty_searchresult_from_search_then_return_empty_list_of_suggestedDocuments()
        {
            var intents = new List<Intent>
            {
                IntentBuilder.Build.WithName("Need Help").Instance
            };

            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intents).Instance;

            this._indexSearchMock
                .Setup(x => x.Search(It.IsAny<string>()))
                .Returns(new SearchResult());

            this._nlpCallMock
                .Setup(x => x.GetNlpAnalysis(It.IsAny<string>()))
                .Returns(nlpAnalysis);

            this._suggestionsService.GetSuggestedDocuments(this.GetConversationContext()).Should().BeEquivalentTo(new List<SuggestedDocument>());
        }

        [Test]
        [TestCase]
        public void When_receive_irrelevant_intent_then_returns_empty_list_of_suggestedDocuments()
        {
            var intents = new List<Intent>
            {
                IntentBuilder.Build.WithName("Greetings").Instance
            };

            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intents).Instance;

            this._nlpCallMock
                .Setup(x => x.GetNlpAnalysis(It.IsAny<string>()))
                .Returns(nlpAnalysis);

            this._suggestionsService.GetSuggestedDocuments(this.GetConversationContext()).Should().BeEquivalentTo(new List<SuggestedDocument>());
        }

        [Test]
        [TestCase]
        public void When_query_is_selected_by_agent_suggestion_is_filter_out()
        {
            var suggestion = ((SuggestionsService)this._suggestionsService).FilterOutChosenSuggestions(
                this.GetSuggestedDocuments(), this.GetQueriesSentByByAgent());

            suggestion.Should().HaveCount(2);
            suggestion.Should().NotContain(this.GetSuggestedDocuments().Find(x =>
                x.Uri == "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm"));
            suggestion.Should().NotContain(this.GetSuggestedDocuments().Find(x =>
                x.Uri == "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm"));
        }

        [Test]
        [TestCase("*#")]
        [TestCase("I*")]
        [TestCase("*like*")]
        [TestCase("I*i*C*")]
        [TestCase("I like C#")]
        public void When_Intent_With_and_without_Wildcard__is_irrelevant(string wildcardString)
        {
            var query = SearchQueryBuilder.Build.WithQuery("I like C#").Instance;
            var intentsFromApi = new List<string>
            {
                wildcardString
            };

            var intentsFromNLP = new List<Intent>
            {
                IntentBuilder.Build.WithName("I like C#").Instance
            };

            this._suggestionsService = new SuggestionsService(this._indexSearchMock.Object, this._nlpCallMock.Object, this._documentFacetsMock.Object, intentsFromApi);

            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intentsFromNLP).Instance;
            this._nlpCallMock
                .Setup(x => x.GetNlpAnalysis(It.IsAny<string>()))
                .Returns(nlpAnalysis);

            ((SuggestionsService)this._suggestionsService).IsQueryRelevant(query).Should().BeFalse();
        }

        [Test]
        [TestCase("smalltalk.*")]
        public void When_Intent_is_relevant(string wildcardString)
        {
            var query = SearchQueryBuilder.Build.WithQuery("I like C#").Instance;
            var intentsFromApi = new List<string>
            {
                wildcardString
            };

            var intentsFromNLP = new List<Intent>
            {
                IntentBuilder.Build.WithName("I like C#").Instance
            };

            var nlpAnalysis = NlpAnalysisBuilder.Build.WithIntents(intentsFromNLP).Instance;
            this._nlpCallMock
                .Setup(x => x.GetNlpAnalysis(It.IsAny<string>()))
                .Returns(nlpAnalysis);

            ((SuggestionsService)this._suggestionsService).IsQueryRelevant(query).Should().BeTrue();
        }

        [Test]
        [TestCase("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d1", "a21d07d5-fd5a-42ab-ac2c-2ef6101e58d2", "a21d07d5-fd5a-42ab-ac2c-2ef6101e58d1")]
        [TestCase("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d1", "a21d07d5-fd5a-42ab-ac2c-2ef6101e58d2", "a21d07d5-fd5a-42ab-ac2c-2ef6101e58d2")]
        public void When_receiving_valid_selectQueryId_add_suggestion_to_list_and_return_true(string suggestedDocumentId, string questionId, string selectQueryId)
        {
            SuggestedDocument suggestedDocument = new SuggestedDocument();
            Question question = QuestionBuilder.Build.Instance;
            suggestedDocument.Id = new Guid(suggestedDocumentId);
            question.Id = new Guid(questionId);

            this._conversationContext.SuggestedDocuments.Add(suggestedDocument);
            this._conversationContext.Questions.Add(question);

            bool isContextUpdated = this._suggestionsService.UpdateContextWithSelectedSuggestion(this._conversationContext, new Guid(selectQueryId));

            Assert.IsTrue(isContextUpdated);
            Assert.IsTrue(this._conversationContext.SelectedQuestions.Contains(question) != this._conversationContext.SelectedSuggestedDocuments.Contains(suggestedDocument));
        }

        [Test]
        [TestCase("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d3")]
        public void When_receiving_invalid_selectQueryId_do_not_add_suggestion_to_list_and_return_false(string selectQueryId)
        {
            this._conversationContext.SelectedSuggestedDocuments.Clear();

            bool isContextUpdated = this._suggestionsService.UpdateContextWithSelectedSuggestion(this._conversationContext, new Guid(selectQueryId));

            Assert.IsFalse(isContextUpdated);
            Assert.IsTrue(this._conversationContext.SelectedSuggestedDocuments.Count == 0);
            Assert.IsTrue(this._conversationContext.SelectedQuestions.Count == 0);
        }

        public List<SearchQuery> GetQueriesSentByByAgent()
        {
            return new List<SearchQuery>
            {
               new SearchQuery
               {
                   ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"),
                   Query = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                   Type = SearchQuery.MessageType.Agent,
                   Relevant = true
               },
               new SearchQuery
               {
                   ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"),
                   Query = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                   Type = SearchQuery.MessageType.Agent,
                   Relevant = true
               }
            };
        }

        public SearchResult GetSearchResult()
        {
            return new SearchResult
            {
                NbrElements = 4,
                Elements = new List<SearchResultElement>
                {
                    new SearchResultElement
                    {
                        Title = "Available Coveo Cloud V2 Source Types",
                        Uri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                        PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                        Summary = null,
                        Score = 4280
                    },
                    new SearchResultElement
                    {
                        Title = "Coveo Cloud Query Syntax Reference",
                        Uri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                        PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                        Summary = null,
                        Score = 3900
                    },
                    new SearchResultElement
                    {
                        Title = "Events",
                        Uri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                        PrintableUri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                        Summary = null,
                        Score = 2947
                    },
                    new SearchResultElement
                    {
                        Title = "Coveo Facet Component (CoveoFacet)",
                        Uri = "https://coveo.github.io/search-ui/components/facet.html",
                        PrintableUri = "https://coveo.github.io/search-ui/components/facet.html",
                        Summary = null,
                        Score = 2932
                    }
                }
            };
        }

        public List<SuggestedDocument> GetSuggestedDocuments()
        {
            return new List<SuggestedDocument>
            {
                new SuggestedDocument
                {
                    Title = "Available Coveo Cloud V2 Source Types",
                    Uri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                    PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Available_Coveo_Cloud_V2_Source_Types.htm",
                    Summary = null
                },
                new SuggestedDocument
                {
                    Title = "Coveo Cloud Query Syntax Reference",
                    Uri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                    PrintableUri = "https://onlinehelp.coveo.com/en/cloud/Coveo_Cloud_Query_Syntax_Reference.htm",
                    Summary = null
                },
                new SuggestedDocument
                {
                    Title = "Events",
                    Uri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                    PrintableUri = "https://developers.coveo.com/display/JsSearchV1/Page/27230520/27230472/27230573",
                    Summary = null
                },
                new SuggestedDocument
                {
                    Title = "Coveo Facet Component (CoveoFacet)",
                    Uri = "https://coveo.github.io/search-ui/components/facet.html",
                    PrintableUri = "https://coveo.github.io/search-ui/components/facet.html",
                    Summary = null
                }
            };
        }

        public List<string> GetIrrelevantIntents()
        {
            return new List<string>
            {
                "Greetings"
            };
        }

        public ConversationContext GetConversationContext()
        {
            ConversationContext context = new ConversationContext(new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), DateTime.Now)
            {
                SearchQueries = new List<SearchQuery>
                {
                    new SearchQuery { ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), Query = "rest api", Type = SearchQuery.MessageType.Customer, Relevant = true }
                }
            };
            return context;
        }

        public ConversationContext GetConversationContextForFilterChosenSuggestions()
        {
            ConversationContext context = new ConversationContext(new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), DateTime.Now)
            {
                SearchQueries = new List<SearchQuery>
                {
                    new SearchQuery { ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), Query = "rest api", Type = SearchQuery.MessageType.Customer, Relevant = true },
                    new SearchQuery { ChatKey = new Guid("0f8fad5b-d9cb-469f-a165-708677289501"), Query = "Available Coveo Cloud V2 Source Types", Type = SearchQuery.MessageType.Agent, Relevant = true }
                }
            };
            return context;
        }
    }
}
