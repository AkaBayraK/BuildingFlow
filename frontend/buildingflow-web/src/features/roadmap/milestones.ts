export type RoadmapTask = { title: string; detail: string; done: boolean; verification?: string };
export type Milestone = { number: string; title: string; description: string; tasks: RoadmapTask[] };

// Mark a task done only after its implementation and relevant checks are complete.
// This is development progress, independent of whether local services are running.
const done = (title: string, detail: string, verification = "verificationRecordDocsTESTINGMd00Foundation"): RoadmapTask => ({ title, detail, done: true, verification });
const planned = (title: string, detail: string): RoadmapTask => ({ title, detail, done: false });

export const milestones: Milestone[] = [
  {
    number: "00", title: "projectFoundationAndLocalEnvironment",
    description: "completedArchitectureDomainFoundationAndLocalRuntime",
    tasks: [
      done("modularMonolithAndProjectLayers", "domainApplicationInfrastructureAPIWorkerAndTestProjectsExist"),
      done("commonPropertyDomainModel", "stage03ExpandedSiteBlockApartmentToPropertyOptional", "17DomainTestsFourPropertyTypesAndApartmentMixed"),
      done("commonBaseEntityAndAuditFields", "allEightFieldsAreNOTNULLInEveryApplication", "modelAndRealPostgreSQLSchemaContractsLatest171Backend"),
      done("postgresqlAndEFCorePersistence", "dbcontextSharedMappingVersionedMigrationsSnapshotRestrictRelationshipsAnd", "realPostgreSQLInitialSetupDataPreservingUpgradeAndRepeat"),
      done("softDeleteAndOptimisticConcurrency", "deletesBecomeSoftDeleteQueryFiltersAndVersionBased"),
      done("apiFoundationStandards", "healthLivenessDevelopmentOpenAPIProblemDetailsCentralErrorHandlingAnd"),
      done("keycloakAndJWTValidationFoundation", "realmInitialRolesIssuerAudienceLifetimeValidationAndCurrent"),
      done("dockerComposeServices", "localConfigurationAndVolumesForWebAPIWorkerPostgreSQL"),
      done("workerStorageAndMessagingFoundation", "workerHostAndContractsExistConsumersAndProvidersBelong"),
      done("frontendShellAndLogo", "nextJsOverviewNavigationSelectedLogoAndAPIHealth", "workspaceFrameVerification"),
      done("initialVerification", "frontendNETBuilds11DomainTests2PersistenceTests"),
      done("localDevelopmentShortcuts", "readmeExampleEnvironmentSecretPasswordGenerationAndVSCode"),
      done("sharedFeedbackTitle", "sharedFeedbackDetail", "sharedFeedbackVerification"),
      done("localeStandardsTitle", "localeStandardsDetail", "localeStandardsVerification"),
      done("separateMenusTitle", "separateMenusDetail", "separateMenusVerification"),
      done("firstGitCommit", "theInitialProjectIsCommittedLocallyRealPasswordsRemain"),
    ],
  },
  {
    number: "01", title: "signInSignOutAndPasswordReset",
    description: "completedTwoColumnIllustratedSignInAndKeycloakIdentity",
    tasks: [
      done("selectedSignInDesign", "navyIllustrationPanelWhiteFormBuildingFlowLogoTurkishFields", "browserSignInDesktopMobileReviewNpmBuildTypecheck"),
      done("secureSignIn", "emailUsernameSignInThroughKeycloakOIDCAuthorizationCode", "authSmokeValidInvalidPasswordPKCEExchangeAndReal"),
      done("sessionProtectionAndTokenRefresh", "anonymousVisitorsGoToSignInTokensStayIn", "browserAnonymousRedirectAuthSmokeRefreshExpiredNETJWT"),
      done("userInformationAndSignOut", "usernameAPICheckAndSignOutControlsSignedOut", "authSmokeLogoutRedirectAndClosedSessionRefreshRejection"),
      done("forgotPasswordAndEmailLink", "keycloakSendsResetEmailToLocalMailpitTheLink", "authSmokeResetRequestMailpitDeliveryAndPasswordForm"),
      done("passwordRulesAndSingleUseLink", "atLeast12CharactersWithUppercaseLowercaseAndDigits", "authSmokeWeakPasswordMismatchedConfirmationUsedLinkAnd"),
      done("apiAuthenticationAndCORS", "apiChecksSignatureIssuerAudienceAndExpiryConfiguredFrontend", "9JWTCORSTestsPlusRealKeycloakTokenAPI"),
      done("localDevelopmentAccount", "randomPasswordStaysInEnvOutsideSourceCodeExisting", "keycloakSetupAnd17AuthSmokeChecksWithA"),
      done("testAgentAndLicenseRecords", "testingMdAGENTSMdSKILLSANDAGENTSMdAnd", "22NETTestsNoneSkipped17AuthSmokeChecks"),
    ],
  },
  {
    number: "02", title: "createAndListTheFirstCommunity",
    description: "completedAuthorizedCreationShadcnUiTanStackTableAndProperty",
    tasks: [
      done("propertyCreationPolicy", "creationRequiresSUPERADMINAnonymousAndInsufficientlyAuthorizedRequests", "siteapitestsAnonymous4017RoleClaimVariants403SUPER"),
      done("backendRoleAndDataAccessRules", "signedKeycloakRealmRolesMapToPoliciesSUPERADMIN", "siteapitestsOutOfScope404ForgedQueriesClaimsAnd"),
      done("propertyCreationApplicationFlow", "nameCityDistrictAddressValidationDTOsCancellationAndAtomic", "realPostgreSQLCreationCreatorMembershipTrimmingAnd8Empty"),
      done("technicalTask1", "stage03MovedCreationIntoTheCommonModelName", "siteapitests201Location5MissingFieldsPropertyModelTestsFourTypes"),
      done("getApiV1PropertiesAndDetail", "serverSearchPermittedSortingAndStablePaginationDeletedOr", "siteapitestsPaginationBoundariesStableEqualNamesLiteralSearchSort"),
      done("propertyCreationForm", "shadcnUiDialogFieldErrorsErrorClearingSubmissionLock", "browser4RequiredErrorsFirstFieldFocusClearedErrors"),
      done("propertyListAndDetailScreen", "tanstackTableShadcnUiSearchSortPageSizeColumn", "browserSearchEmptyResultAddressColumnSorting20Row"),
      done("verifiedAuditAndVersion", "auditUsesJWTSubTheClientCannotSetAudit", "siteapitestsTrustedAuditMassAssignmentRejectionImmutableAuditAnd"),
      done("apiAndEndToEndVerification", "propertyDataIsAbsentFromAnonymousSSRSignIn", "56NETTestsNoneSkipped17KeycloakChecksAnonymous"),
      done("documentationAndCompletionMarkers", "readmeTESTINGEndpointsAgentStatusAndLicensesUpdatedVerified", "frontendTypecheckBuild262LicensedPackageVersionsOfficialMIT"),
    ],
  },
  {
    number: "03", title: "propertiesBuildingsAndUnits", description: "completedDirectUnitsForApartmentsBuildingsBlocksAndUnits",
    tasks: [
      done("commonPropertyBuildingUnitModel", "fourPropertyTypesShareTheModelApartmentUnitsHave", "propertymodeltestsPropertyTypeChangeTestsHierarchyUnrelatedTypeEditsActiveInactiveDeleted"),
      done("dataPreservingMigrationAndSchema", "renameBackfillToPropertiesBuildingsUnitsPropertyMembershipsEight", "realPostgreSQLSetupUpgradeRepeatSafeDownDataLoss"),
      done("createAndManageBuildingsBlocks", "nameAndOptionalFloorCountListSearchSortEdit", "hierarchyapitestsCRUDThreeWriterRolesScopeDuplicatesAndPagination"),
      done("manageApartmentsAndUnits", "directApartmentsOrBuildingScopedApartmentOfficeShopVilla", "realPostgreSQLSixUnitTypesDirectCRUDUniqueComposite"),
      done("versionStatusAndHistoryPreservingDeletion", "staleVersionsReturn409WithReloadNonemptyParentsCannot", "apiDuplicateParentRacesAndRollbackTwoBrowserSessions"),
      done("uiRegressionAndDeliveryDocuments", "propertyUnitDetailsFrontendDelivery", "propertyDetailsFrontendVerification"),
    ],
  },
  {
    number: "04", title: "peopleAndUnitRelationships", description: "completedPeopleDatedOwnerTenantResidentRelationshipsAndMove",
    tasks: [
      done("personModelAndRecordFlows", "propertyScopedNamesAndOptionalContactCRUDSearchSort", "personProfileVerification"),
      done("unitoccupancyRelationships", "realUnitOwnerTenantResidentLinksInclusiveDatesSame", "7NewDomainAnd18RealPostgreSQLAPICases"),
      done("moveInAndHistoryScreens", "unitPeopleNavigationDetail", "unitPeopleNavigationVerification"),
      done("privacyMigrationAndRegression", "managementRolesAndActiveMembershipProtectPersonalDataOngoing", "171NETTests28Unit143IntegrationNoneFailed"),
    ],
  },
  {
    number: "05", title: "duesAndCharges", description: "consistentPeriodicAssessmentsAndUnitBalances",
    tasks: [
      done("duesAndDebtDomainRules", "definePeriodAmountCurrencyDueDateStatusPrecisionAnd", "phase05Verification"),
      done("createAssessments", "authorizedUnitAssessmentsWithProtectionAgainstDuplicatePeriods", "phase05Verification"),
      done("debtListAndFilters", "propertyUnitPeriodAndDueDateFiltersDebtSummaries", "phase05Verification"),
      done("transactionIntegrityTests", "verifyBulkAssessmentsRepeatedRequestsCalculationsAndFinancialPermissions", "phase05Verification"),
      done("duesBulkSelectionTitle", "duesBulkSelectionDetail", "duesBulkSelectionVerification"),
    ],
  },
  {
    number: "06", title: "paymentsAndDebtSettlement", description: "linkReceiptsToDebtsAndPreserveTransactionHistory",
    tasks: [
      done("paymentRecordsAndAllocation", "amountDateMethodReferencePartialPaymentsBalanceAndAllocation", "phase06Verification"),
      done("duplicateRequestsAndConcurrency", "preventDuplicatePaymentsTransactionalConcurrentBalanceProtection", "phase06Verification"),
      done("cancellationRefundsAndReceipts", "correctFinancialRecordsWithoutDeletingHistoryDesignReceiptViewing", "phase06Verification"),
      done("paymentScreensAndTests", "verifyFormsHistorySettlementAndPermissionsSelectAnOnline", "phase06Verification"),
    ],
  },
  {
    number: "06.1", title: "phase061Title", description: "phase061Description",
    tasks: [
      done("phase061Parties", "phase061PartiesDetail", "phase061Verification"),
      done("phase061Filters", "phase061FiltersDetail", "phase061Verification"),
      done("phase061Approval", "phase061ApprovalDetail", "phase061Verification"),
      done("phase061Tests", "phase061TestsDetail", "phase061Verification"),
    ],
  },
  {
    number: "07", title: "expensesAndBudget", description: "classifyExpensesAndCompleteTheFinancialOverview",
    tasks: [
      done("expenseModelAndCategories", "categoryAmountDateDescriptionAndDocumentsFinanciallyScopedAccess", "phase07Verification"),
      done("expenseManagementScreens", "createListFilterAndPreserveHistoryThroughCorrections", "phase07Verification"),
      done("incomeExpensesAndBudgetSummary", "compareActualPeriodIncomeExpensesAndBudgets", "phase07Verification"),
      done("financialVerification", "testCalculationsPermissionsConcurrencyAndReportConsistency", "phase07Verification"),
    ],
  },
  {
    number: "07.1", title: "phase071Title", description: "phase071Description",
    tasks: [
      done("phase071Analysis", "phase071AnalysisDetail", "phase071AnalysisVerification"),
      done("phase071Catalog", "phase071CatalogDetail", "phase071Verification"),
      done("phase071I18n", "phase071I18nDetail", "phase071Verification"),
      done("phase071Notifications", "phase071NotificationsDetail", "phase071Verification"),
      done("phase071Tests", "phase071TestsDetail", "phase071Verification"),
    ],
  },
  {
    number: "08", title: "announcementsAndFiles", description: "communityCommunicationAndSecureDocumentStorage",
    tasks: [
      done("publishAnnouncements", "titleContentAudienceAndPublicationStatusCreateListScreens", "phase08Verification"),
      done("seaweedfsStorageProvider", "implementIFileStorageBucketObjectKeysAndDatabaseMetadataOnly", "phase08Verification"),
      done("secureFileAccess", "validateSizeTypeScopeAuthorizedDownloadsAndDeletion", "phase08Verification"),
      done("attachmentsAndIntegrationTests", "connectAnnouncementsDocumentsVerifySeaweedFSUploadDownloadAndUnauthorized", "phase08Verification"),
    ],
  },
  {
    number: "09", title: "asynchronousEmailSMSAndNotifications", description: "reliableRabbitMQAndWorkerBackgroundProcessing",
    tasks: [
      done("messageContractsAndPublisher", "defineEmailRequestedSmsRequestedNotificationRequestedWithCorrelationIDsAndA", "phase09Step01Verification"),
      done("consistentPublicationAndConsumers", "ensureDatabaseMessageConsistencyWithAJustifiedOutboxImplement", "phase09Step02Verification"),
      done("retryDeadLetterAndIdempotency", "retryTransientFailuresRouteFailedMessagesToDeadLetter", "phase09Step03Verification"),
      done("providerIntegrationAndTests", "integrateSelectedEmailSMSProvidersWithExternalSecretsVerify", "phase09Step04Verification"),
    ],
  },
  {
    number: "10", title: "reportsMetersAndObservability", description: "turnOperationsDataIntoReportsAndDiagnosticTools",
    tasks: [
      done("metersAndReadings", "meterModelDatedReadingsCorrectionsConsumptionAndHistoryScreens", "phase10MetersVerification"),
      done("managementReports", "scopeOccupancyDebtReceiptsExpensesAndConsumptionReportsTo", "phase10ReportsVerification"),
      done("prometheusAndGrafana", "metricsExporterDataSourceAndRequestErrorWorkerDashboards", "phase10MetricsVerification"),
      done("lokiAndOpenTelemetry", "sendLogsToLokiWithCorrelationAndOptionalTraces", "phase10LogsVerification"),
    ],
  },
  {
    number: "11", title: "releaseReadinessAndQuality", description: "prepareCompletedWorkflowsForASecureMaintainableRelease",
    tasks: [
      done("securityAndAccessReview", "httpsSecureSessionsCookiesCORSNecessaryRateLimitsAnd", "phase11Step01Verification"),
      done("ciAndAutomatedChecks", "buildUnitRelationalTestsDependencyChecksAndMigrationDeployment", "phase11Step02Verification"),
      done("backupAndRestore", "planAndVerifyPostgreSQLKeycloakAndFileBackupsRestoration", "phase11Step03Verification"),
      done("endToEndAcceptanceAndRelease", "verifyCoreJourneysAccessibilityErrorsCompleteOperationsUserDocs", "phase11Step04Verification"),
    ],
  },
  {
    number: "12", title: "phase12Title", description: "phase12Description",
    tasks: [
      planned("phase12Analysis", "phase12AnalysisDetail"),
      planned("phase12PersonCode", "phase12PersonCodeDetail"),
      planned("phase12SmsManagement", "phase12SmsManagementDetail"),
      planned("phase12RegistrationSms", "phase12RegistrationSmsDetail"),
      planned("phase12UserManagement", "phase12UserManagementDetail"),
      planned("phase12MobileSignup", "phase12MobileSignupDetail"),
      planned("phase12CodeLink", "phase12CodeLinkDetail"),
      planned("phase12Tests", "phase12TestsDetail"),
    ],
  },
];
