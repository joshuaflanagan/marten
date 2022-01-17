﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Marten.Events.Projections;
using Marten.Linq;
using Marten.Services;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.Events.Projections
{
    // License events
    public class LicenseCreated
    {
        public Guid LicenseId { get; }

        public string Name { get; }

        public LicenseCreated(Guid licenseId, string name)
        {
            LicenseId = licenseId;
            Name = name;
        }
    }

    public class LicenseFeatureToggled
    {
        public Guid LicenseId { get; }

        public string FeatureToggleName { get; }

        public LicenseFeatureToggled(Guid licenseId, string featureToggleName)
        {
            LicenseId = licenseId;
            FeatureToggleName = featureToggleName;
        }
    }

    // User Groups events

    public class UserGroupCreated
    {
        public Guid GroupId { get; }

        public string Name { get; }

        public UserGroupCreated(Guid groupId, string name)
        {
            GroupId = groupId;
            Name = name;
        }
    }

    public class UsersAssignedToGroup
    {
        public Guid GroupId { get; }

        public List<Guid> UserIds { get; }

        public UsersAssignedToGroup(Guid groupId, List<Guid> userIds)
        {
            GroupId = groupId;
            UserIds = userIds;
        }
    }

    // User Events
    public class UserRegistered
    {
        public Guid UserId { get; }

        public string Email { get; }

        public UserRegistered(Guid userId, string email)
        {
            UserId = userId;
            Email = email;
        }
    }

    public class UserLicenseAssigned
    {
        public Guid UserId { get; }

        public Guid LicenseId { get; }

        public UserLicenseAssigned(Guid userId, Guid licenseId)
        {
            UserId = userId;
            LicenseId = licenseId;
        }
    }

    public class UserFeatureToggles
    {
        public Guid Id { get; set; }

        public Guid LicenseId { get; set; }

        public List<string> FeatureToggles { get; set; } = new List<string>();
    }

    // projection with documentsession
    public class UserFeatureTogglesProjection: ViewProjection<UserFeatureToggles, Guid>
    {
        public UserFeatureTogglesProjection()
        {
            ProjectEvent<UserRegistered>(x => x.UserId, Apply);
            ProjectEvent<UserLicenseAssigned>(x => x.UserId, Apply);
            ProjectEvent<LicenseFeatureToggled>(FindUserIdsWithLicense, Apply);
        }

        private static List<Guid> FindUserIdsWithLicense(IDocumentSession session, LicenseFeatureToggled @event)
        {
            return session.Query<UserFeatureToggles>()
                .Where(x => x.LicenseId == @event.LicenseId)
                .Select(x => x.Id)
                .ToList();
        }

        private void Apply(UserFeatureToggles view, UserRegistered @event)
        {
            view.Id = @event.UserId;
        }

        private void Apply(UserFeatureToggles view, UserLicenseAssigned @event)
        {
            view.LicenseId = @event.LicenseId;
        }

        private void Apply(UserFeatureToggles view, LicenseFeatureToggled @event)
        {
            view.FeatureToggles.Add(@event.FeatureToggleName);
        }
    }

    public class UserGroupsAssignment
    {
        public Guid Id { get; set; }

        public List<Guid> Groups { get; set; } = new List<Guid>();
    }

    public class UserGroupsAssignmentProjection: ViewProjection<UserGroupsAssignment, Guid>
    {
        public UserGroupsAssignmentProjection()
        {
            ProjectEvent<UserRegistered>(x => x.UserId, Apply);
            ProjectEvent<UsersAssignedToGroup>(x => x.UserIds, Apply);
        }

        private void Apply(UserGroupsAssignment view, UserRegistered @event)
        {
            view.Id = @event.UserId;
        }

        private void Apply(UserGroupsAssignment view, UsersAssignedToGroup @event)
        {
            view.Groups.Add(@event.GroupId);
        }
    }

    public class multiple_streams_projections: IntegrationContextWithIdentityMap<IdentityMap>
    {
        [Fact]
        public async Task multi_stream_projections_should_work()
        {
            StoreOptions(_ =>
            {
                _.AutoCreateSchemaObjects = AutoCreate.All;

                _.Events.InlineProjections.Add<UserFeatureTogglesProjection>();
                _.Events.InlineProjections.Add<UserGroupsAssignmentProjection>();
            });

            // --------------------------------
            // Create Licenses
            // --------------------------------
            // Free License
            // Premium License
            // --------------------------------

            var freeLicenseCreated = new LicenseCreated(Guid.NewGuid(), "Free Licence");
            theSession.Events.Append(freeLicenseCreated.LicenseId, freeLicenseCreated);

            var premiumLicenseCreated = new LicenseCreated(Guid.NewGuid(),"Premium Licence");
            theSession.Events.Append(premiumLicenseCreated.LicenseId, premiumLicenseCreated);

            await theSession.SaveChangesAsync();

            // --------------------------------
            // Create Groups
            // --------------------------------
            // Regular Users
            // Admin Users
            // --------------------------------

            var regularUsersGroupCreated = new UserGroupCreated(Guid.NewGuid(), "Regular Users");
            theSession.Events.Append(regularUsersGroupCreated.GroupId, regularUsersGroupCreated);

            var adminUsersGroupCreated = new UserGroupCreated(Guid.NewGuid(),"Admin Users");
            theSession.Events.Append(adminUsersGroupCreated.GroupId, adminUsersGroupCreated);

            await theSession.SaveChangesAsync();

            // --------------------------------
            // Create Users
            // --------------------------------
            // Anna
            // John
            // Maggie
            // Alan
            // --------------------------------

            var annaRegistered = new UserRegistered(Guid.NewGuid(), "Anna");
            theSession.Events.Append(annaRegistered.UserId, annaRegistered);

            var johnRegistered =  new UserRegistered(Guid.NewGuid(), "John");
            theSession.Events.Append(johnRegistered.UserId, johnRegistered);

            var maggieRegistered =  new UserRegistered(Guid.NewGuid(), "Maggie");
            theSession.Events.Append(maggieRegistered.UserId, maggieRegistered);

            var alanRegistered =  new UserRegistered(Guid.NewGuid(), "Alan");
            theSession.Events.Append(alanRegistered.UserId, alanRegistered);

            await theSession.SaveChangesAsync();

            // --------------------------------
            // Assign users' licences
            // --------------------------------
            // Anna, Maggie => Premium
            // John, Alan   => Free
            // --------------------------------

            var annaAssignedToPremiumLicense = new UserLicenseAssigned(annaRegistered.UserId, premiumLicenseCreated.LicenseId);
            theSession.Events.Append(annaRegistered.UserId, annaAssignedToPremiumLicense);

            var johnAssignedToFreeLicense = new UserLicenseAssigned(johnRegistered.UserId, freeLicenseCreated.LicenseId);
            theSession.Events.Append(johnRegistered.UserId, johnAssignedToFreeLicense);

            var maggieAssignedToPremiumLicense = new UserLicenseAssigned(maggieRegistered.UserId, premiumLicenseCreated.LicenseId);
            theSession.Events.Append(maggieAssignedToPremiumLicense.UserId, maggieAssignedToPremiumLicense);

            var alanAssignedToFreeLicense = new UserLicenseAssigned(alanRegistered.UserId, freeLicenseCreated.LicenseId);
            theSession.Events.Append(alanAssignedToFreeLicense.UserId, alanAssignedToFreeLicense);

            await theSession.SaveChangesAsync();

            // --------------------------------
            // Assign feature toggle to license
            // --------------------------------
            // Login  => Free, Premium
            // Invite => Premium
            // --------------------------------

            var loginFeatureToggle = "Login";
            var loginFeatureToggledOnFreeLicense = new LicenseFeatureToggled(freeLicenseCreated.LicenseId, loginFeatureToggle);
            theSession.Events.Append(loginFeatureToggledOnFreeLicense.LicenseId, loginFeatureToggledOnFreeLicense);

            var loginFeatureToggledOnPremiumLicense = new LicenseFeatureToggled(premiumLicenseCreated.LicenseId, loginFeatureToggle);
            theSession.Events.Append(loginFeatureToggledOnPremiumLicense.LicenseId, loginFeatureToggledOnPremiumLicense);

            var inviteFeatureToggle = "Invite";
            var inviteToggledOnPremiumLicense = new LicenseFeatureToggled(premiumLicenseCreated.LicenseId, inviteFeatureToggle);
            theSession.Events.Append(inviteToggledOnPremiumLicense.LicenseId, inviteToggledOnPremiumLicense);

            await theSession.SaveChangesAsync();

            // --------------------------------
            // Check users' feature toggles
            // --------------------------------
            // Anna, Maggie => Premium => Login, Invite
            // John, Alan   => Free    => Login
            // --------------------------------

            var annaFeatureToggles = await theSession.LoadAsync<UserFeatureToggles>(annaRegistered.UserId);
            annaFeatureToggles.Id.ShouldBe(annaRegistered.UserId);
            annaFeatureToggles.LicenseId.ShouldBe(premiumLicenseCreated.LicenseId);
            annaFeatureToggles.FeatureToggles.ShouldHaveTheSameElementsAs(loginFeatureToggle, inviteFeatureToggle);

            var maggieFeatureToggles = await theSession.LoadAsync<UserFeatureToggles>(maggieRegistered.UserId);
            maggieFeatureToggles.Id.ShouldBe(maggieRegistered.UserId);
            maggieFeatureToggles.LicenseId.ShouldBe(premiumLicenseCreated.LicenseId);
            maggieFeatureToggles.FeatureToggles.ShouldHaveTheSameElementsAs(loginFeatureToggle, inviteFeatureToggle);

            var johnFeatureToggles = await theSession.LoadAsync<UserFeatureToggles>(johnRegistered.UserId);
            johnFeatureToggles.Id.ShouldBe(johnRegistered.UserId);
            johnFeatureToggles.LicenseId.ShouldBe(freeLicenseCreated.LicenseId);
            johnFeatureToggles.FeatureToggles.ShouldHaveTheSameElementsAs(loginFeatureToggle);

            var alanFeatureToggles = await theSession.LoadAsync<UserFeatureToggles>(alanRegistered.UserId);
            alanFeatureToggles.Id.ShouldBe(alanRegistered.UserId);
            alanFeatureToggles.LicenseId.ShouldBe(freeLicenseCreated.LicenseId);
            alanFeatureToggles.FeatureToggles.ShouldHaveTheSameElementsAs(loginFeatureToggle);

            // --------------------------------
            // Assign users to Groups
            // --------------------------------
            // Anna, Maggie => Admin
            // John, Alan   => Regular
            // --------------------------------

            var annaAndMaggieAssignedToAdminUsersGroup = new UsersAssignedToGroup(adminUsersGroupCreated.GroupId,
                new List<Guid> {annaRegistered.UserId, maggieRegistered.UserId});
            theSession.Events.Append(annaAndMaggieAssignedToAdminUsersGroup.GroupId, annaAndMaggieAssignedToAdminUsersGroup);

            var johnAndAlanAssignedToRegularUsersGroup = new UsersAssignedToGroup(regularUsersGroupCreated.GroupId,
                new List<Guid> {johnRegistered.UserId, alanRegistered.UserId});
            theSession.Events.Append(johnAndAlanAssignedToRegularUsersGroup.GroupId, johnAndAlanAssignedToRegularUsersGroup);

            await theSession.SaveChangesAsync();

            // --------------------------------
            // Check users' groups assignment
            // --------------------------------
            // Anna, Maggie => Admin
            // John, Alan   => Regular
            // --------------------------------

            var annaGroupAssignment = await theSession.LoadAsync<UserGroupsAssignment>(annaRegistered.UserId);
            annaGroupAssignment.Id.ShouldBe(annaRegistered.UserId);
            annaGroupAssignment.Groups.ShouldHaveTheSameElementsAs(adminUsersGroupCreated.GroupId);

            var maggieGroupAssignment = await theSession.LoadAsync<UserGroupsAssignment>(maggieRegistered.UserId);
            maggieGroupAssignment.Id.ShouldBe(maggieRegistered.UserId);
            maggieGroupAssignment.Groups.ShouldHaveTheSameElementsAs(adminUsersGroupCreated.GroupId);

            var johnGroupAssignment = await theSession.LoadAsync<UserGroupsAssignment>(johnRegistered.UserId);
            johnGroupAssignment.Id.ShouldBe(johnRegistered.UserId);
            johnGroupAssignment.Groups.ShouldHaveTheSameElementsAs(regularUsersGroupCreated.GroupId);

            var alanGroupAssignment = await theSession.LoadAsync<UserGroupsAssignment>(alanRegistered.UserId);
            alanGroupAssignment.Id.ShouldBe(alanRegistered.UserId);
            alanGroupAssignment.Groups.ShouldHaveTheSameElementsAs(regularUsersGroupCreated.GroupId);
        }

        public multiple_streams_projections(DefaultStoreFixture fixture) : base(fixture)
        {
        }
    }
}