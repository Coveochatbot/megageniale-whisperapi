﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using WhisperAPI.Models;
using WhisperAPI.Services;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class ContextsTest
    {
        private readonly Contexts _contexts;

        public ContextsTest()
        {
            this._contexts = new Contexts(new DbContextOptionsBuilder<Contexts>().UseInMemoryDatabase("contextDB").Options, new TimeSpan(1, 0, 0, 0));
        }

        [Test]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        public void When_adding_duplicate_suggestion_only_one_is_added(string chatkey)
        {
            ConversationContext conversationcontext = this._contexts[new Guid(chatkey)];
            SuggestedDocument query1 = this.GetSuggestedDocument();
            SuggestedDocument query2 = this.GetSuggestedDocument();
            conversationcontext.SuggestedDocuments.Add(query1);
            conversationcontext.SuggestedDocuments.Add(query2);
            conversationcontext.SuggestedDocuments.Should().HaveCount(1);
        }

        [Test]
        [Order(0)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289502")]
        public void When_retrieving_non_existing_conversation_context_then_return_new_one(string chatkey)
        {
            ConversationContext conversationcontext = this._contexts[new Guid(chatkey)];
            this._contexts.SaveChangesAsync();

            conversationcontext.ChatKey.Should().Be(chatkey);
        }

        [Test]
        [Order(1)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289502")]
        public void When_retrieving_existing_conversation_context_then_return_the_context(string chatkey)
        {
            ConversationContext conversationcontext = this._contexts[new Guid(chatkey)];
            conversationcontext.ChatKey.Should().Be(chatkey);
        }

        [Test]
        [Order(2)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289502")]
        public void When_life_span_is_not_expired_then_context_is_not_deleted(string chatkey)
        {
            IEnumerable<ConversationContext> removedContext = this._contexts.RemoveOldContext();

            removedContext.Should().BeEmpty();
        }

        [Test]
        [Order(3)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        public void When_life_span_expired_then_context_is_deleted(string chatkey)
        {
            ConversationContext conversationcontext = this._contexts[new Guid(chatkey)];
            conversationcontext.StartDate = conversationcontext.StartDate.Subtract(new TimeSpan(2, 0, 0, 0));

            this._contexts.SaveChangesAsync();
            IEnumerable<ConversationContext> removedContext = this._contexts.RemoveOldContext();

            removedContext.Should().OnlyContain(x => x.Equals(conversationcontext));
        }

        [Test]
        [Order(4)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        public void When_adding_received_message_to_context_messages_concats_and_persists(string chatkey)
        {

            ConversationContext conversationcontext = this._contexts[new Guid(chatkey)];
            conversationcontext.SearchQuerries.Add(this.GetSearchQuerry("rest api", chatkey));
            this._contexts.SaveChanges();

            conversationcontext = this._contexts[new Guid(chatkey)];
            conversationcontext.SearchQuerries[0].Querry.Should().Be("rest api");

            conversationcontext.SearchQuerries.Add(this.GetSearchQuerry("framework", chatkey));
            this._contexts.SaveChanges();

            conversationcontext = this._contexts[new Guid(chatkey)];
            conversationcontext.SearchQuerries[0].Querry.Should().Be("rest api");
            conversationcontext.SearchQuerries[1].Querry.Should().Be("framework");
        }

        private SearchQuerry GetSearchQuerry(string querry, string chatkey)
        {
            return new SearchQuerry
            {
                ChatKey = new Guid(chatkey),
                Querry = querry,
                Type = SearchQuerry.MessageType.Customer
            };
        }

        private SuggestedDocument GetSuggestedDocument()
        {
            return new SuggestedDocument()
            {
                Title = "title",
                PrintableUri = "www.test.com",
                Uri = "www.test.com",
                Summary = "this is a summary"
            };
        }
    }
}
