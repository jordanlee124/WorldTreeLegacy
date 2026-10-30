using System;
using System.Collections.Generic;
using NUnit.Framework;
using Worldtree.Application.Events;
using Worldtree.Application.Profiles;
using Worldtree.Domain.Profiles;

namespace Worldtree.Tests
{
    /// <summary>Application layer only: an in-memory store, no files, no scenes.</summary>
    public class ProfileServiceTests
    {
        sealed class FakeStore : IProfileStore
        {
            public ProfileLoadResult NextLoad;
            public readonly List<CharacterProfile> Saved = new List<CharacterProfile>();
            public ProfileLoadResult Load() => NextLoad;
            public void Save(CharacterProfile profile) => Saved.Add(profile);
        }

        FakeStore store;
        EventBus bus;
        ProfileService service;
        readonly List<object> events = new List<object>();

        [SetUp]
        public void SetUp()
        {
            store = new FakeStore();
            bus = new EventBus();
            events.Clear();
            bus.Subscribe<ProfileLoaded>(e => events.Add(e));
            bus.Subscribe<ProfileLoadFailed>(e => events.Add(e));
            bus.Subscribe<ProfileSaved>(e => events.Add(e));
            service = new ProfileService(store, bus);
        }

        [Test]
        public void Load_Success_SetsCurrent_AndPublishesLoaded()
        {
            var profile = CharacterProfileTests.Valid();
            store.NextLoad = ProfileLoadResult.Ok(profile, fromBackup: true);
            Assert.AreEqual(ProfileLoadStatus.Loaded, service.Load().Status);
            Assert.AreSame(profile, service.Current);
            var loaded = (ProfileLoaded)events[0];
            Assert.AreSame(profile, loaded.Profile);
            Assert.IsTrue(loaded.FromBackup);
        }

        [Test]
        public void Load_Failure_LeavesCurrentEmpty_AndPublishesFailed()
        {
            store.NextLoad = ProfileLoadResult.Fail(ProfileLoadStatus.Corrupt, "bad");
            Assert.AreEqual(ProfileLoadStatus.Corrupt, service.Load().Status);
            Assert.IsNull(service.Current);
            var failed = (ProfileLoadFailed)events[0];
            Assert.AreEqual(ProfileLoadStatus.Corrupt, failed.Status);
            Assert.AreEqual("bad", failed.Error);
        }

        [Test]
        public void Save_Valid_WritesSetsCurrent_AndPublishesSaved()
        {
            var profile = CharacterProfileTests.Valid();
            service.Save(profile);
            CollectionAssert.AreEqual(new[] { profile }, store.Saved);
            Assert.AreSame(profile, service.Current);
            Assert.AreSame(profile, ((ProfileSaved)events[0]).Profile);
        }

        [Test]
        public void Save_Invalid_Throws_WritesNothing_PublishesNothing()
        {
            Assert.Throws<ArgumentException>(() => service.Save(CharacterProfileTests.Valid(level: 0)));
            Assert.Throws<ArgumentNullException>(() => service.Save(null));
            Assert.IsEmpty(store.Saved);
            Assert.IsEmpty(events);
            Assert.IsNull(service.Current);
        }
    }
}
