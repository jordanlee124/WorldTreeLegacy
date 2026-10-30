using System;
using Worldtree.Application.Events;
using Worldtree.Domain.Profiles;

namespace Worldtree.Application.Profiles
{
    /// <summary>Owns the active character profile: loads and saves it through the store and announces the outcome.</summary>
    public sealed class ProfileService
    {
        readonly IProfileStore store;
        readonly EventBus events;

        public CharacterProfile Current { get; private set; }

        public ProfileService(IProfileStore store, EventBus events)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public ProfileLoadResult Load()
        {
            var result = store.Load();
            if (result.Status == ProfileLoadStatus.Loaded)
            {
                Current = result.Profile;
                events.Publish(new ProfileLoaded(result.Profile, result.FromBackup));
            }
            else
            {
                events.Publish(new ProfileLoadFailed(result.Status, result.Error));
            }
            return result;
        }

        /// <summary>Throws ArgumentException for an invalid profile; store exceptions propagate unchanged.</summary>
        public void Save(CharacterProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var error = profile.Validate();
            if (error != null) throw new ArgumentException(error, nameof(profile));
            store.Save(profile);
            Current = profile;
            events.Publish(new ProfileSaved(profile));
        }
    }
}
