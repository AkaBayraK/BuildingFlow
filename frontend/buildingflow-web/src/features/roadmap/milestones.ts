export type RoadmapTask = { title: string; detail: string; done: boolean; verification?: string };
export type Milestone = { number: string; title: string; description: string; tasks: RoadmapTask[] };

// Mark a task done only after its implementation and relevant checks are complete.
// This is development progress, independent of whether local services are running.
const done = (title: string, detail: string, verification = "verificationRecordDocsTESTINGMd00Foundation"): RoadmapTask => ({ title, detail, done: true, verification });
const planned = (title: string, detail: string): RoadmapTask => ({ title, detail, done: false });

export const milestones: Milestone[] = [
  {number:"SUP-06",title:"supplierWorkOrdersRoadmapTitle",description:"supplierWorkOrdersRoadmapDescription",tasks:[done("supplierWorkOrdersRoadmapTask","supplierWorkOrdersRoadmapDetail","supplierWorkOrdersRoadmapVerification")]},
  {number:"SUP-05",title:"supplierQuotesTitle",description:"supplierQuotesDescription",tasks:[done("supplierQuotesTask","supplierQuotesDetail","supplierQuotesVerification")]},
  {number:"SUP-04",title:"supplierDocumentsTitle",description:"supplierDocumentsDescription",tasks:[done("supplierDocumentsTask","supplierDocumentsDetail","supplierDocumentsVerification")]},

  {number:"SUP-03",title:"supplierRelationsTitle",description:"supplierRelationsDescription",tasks:[done("supplierRelationsTask","supplierRelationsDetail","supplierRelationsVerification")]},

  {number:"SUP-02",title:"supplierUiTitle",description:"supplierUiDescription",tasks:[done("supplierUiTask","supplierUiDetail","supplierUiVerification")]},

  {number:"SUP-01",title:"supplierFoundationTitle",description:"supplierFoundationDescription",tasks:[done("supplierFoundationTask","supplierFoundationDetail","supplierFoundationVerification")]},

  {
    number: "OA-07", title: "commonAreaPhase07Title", description: "commonAreaPhase07Description",
    tasks: [done("commonAreaPhase07Task", "commonAreaPhase07Detail", "commonAreaPhase07Verification")],
  },
  {
    number: "OA-06", title: "commonAreaPhase06Title", description: "commonAreaPhase06Description",
    tasks: [done("commonAreaPhase06Task", "commonAreaPhase06Detail", "commonAreaPhase06Verification")],
  },
  {
    number: "OA-05", title: "commonAreaPhase05Title", description: "commonAreaPhase05Description",
    tasks: [done("commonAreaPhase05Task", "commonAreaPhase05Detail", "commonAreaPhase05Verification")],
  },
  {
    number: "OA-02", title: "commonAreaPhase02Title", description: "commonAreaPhase02Description",
    tasks: [done("commonAreaPhase02Task", "commonAreaPhase02Detail", "commonAreaPhase02Verification")],
  },
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
      done("requestManagementTitle", "requestManagementDetail", "requestManagementVerification"),
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
      done("phase12Task1", "phase12Task1Detail", "phase12Verification"),
      done("phase12Task2", "phase12Task2Detail", "phase12Verification"),
      done("phase12Task3", "phase12Task3Detail", "phase12Verification"),
      done("phase12Task4", "phase12Task4Detail", "phase12Verification"),
      done("phase12Task5", "phase12Task5Detail", "phase12AcceptanceVerification"),
    ],
  },
  {
    number: "13", title: "phase13Title", description: "phase13Description",
    tasks: [
      done("phase13Task1", "phase13Task1Detail", "phase13Verification"),
      done("phase13Task2", "phase13Task2Detail", "phase13Verification"),
      done("phase13Task3", "phase13Task3Detail", "phase13Verification"),
      done("phase13Task4", "phase13Task4Detail", "phase13Verification"),
      done("phase13Task5", "phase13Task5Detail", "phase13Verification"),
      done("phase13Task6", "phase13Task6Detail", "phase13AcceptanceVerification"),
    ],
  },
  {
    number: "14", title: "phase14Title", description: "phase14Description",
    tasks: [
      done("phase14Task1", "phase14Task1Detail", "phase14Verification"),
      done("phase14Task2", "phase14Task2Detail", "phase14Verification"),
      done("phase14Task3", "phase14Task3Detail", "phase14Verification"),
      done("phase14Task4", "phase14Task4Detail", "phase14Verification"),
      done("phase14Task5", "phase14Task5Detail", "phase14Verification"),
      done("phase14Task6", "phase14Task6Detail", "phase14AcceptanceVerification"),
    ],
  },
  {
    number: "15", title: "phase15Title", description: "phase15Description",
    tasks: [
      done("phase15Task1", "phase15Task1Detail", "phase15Verification"),
      done("phase15Task2", "phase15Task2Detail", "phase15Verification"),
      done("phase15Task3", "phase15Task3Detail", "phase15Verification"),
      done("phase15Task4", "phase15Task4Detail", "phase15Verification"),
      done("phase15Task5", "phase15Task5Detail", "phase15Verification"),
      planned("phase15Task6", "phase15Task6Detail"),
    ],
  },
  {
    number: "16", title: "phase16Title", description: "phase16Description",
    tasks: [
      done("phase16Task1", "phase16Task1Detail", "phase16Verification"),
      done("phase16Task2", "phase16Task2Detail", "phase16Verification"),
      done("phase16Task3", "phase16Task3Detail", "phase16Verification"),
      done("phase16Task4", "phase16Task4Detail", "phase16Verification"),
      done("phase16Task5", "phase16Task5Detail", "phase16Verification"),
      planned("phase16Task6", "phase16Task6Detail"),
    ],
  },
  {
    number: "17", title: "phase17Title", description: "phase17Description",
    tasks: [
      done("phase17Task1", "phase17Task1Detail", "phase17Verification"),
      done("phase17Task2", "phase17Task2Detail", "phase17Verification"),
      done("phase17Task3", "phase17Task3Detail", "phase17Verification"),
      done("phase17Task4", "phase17Task4Detail", "phase17Verification"),
      done("phase17Task5", "phase17Task5Detail", "phase17Verification"),
      planned("phase17Task6", "phase17Task6Detail"),
    ],
  },
  {
    number: "18", title: "phase18Title", description: "phase18Description",
    tasks: [
      done("phase18Task1", "phase18Task1Detail", "phase18Verification"),
      done("phase18Task2", "phase18Task2Detail", "phase18Verification"),
      done("phase18Task3", "phase18Task3Detail", "phase18Verification"),
      done("phase18Task4", "phase18Task4Detail", "phase18Verification"),
      done("phase18Task5", "phase18Task5Detail", "phase18Verification"),
      planned("phase18Task6", "phase18Task6Detail"),
    ],
  },
  {
    number: "19", title: "phase19Title", description: "phase19Description",
    tasks: [
      done("phase19Task1", "phase19Task1Detail", "phase19Verification"),
      done("phase19Task2", "phase19Task2Detail", "userPhotoManagementVerification"),
      done("phase19Task3", "phase19Task3Detail", "phase19Verification"),
      done("phase19Task4", "phase19Task4Detail", "phase19Verification"),
      done("phase19Task5", "phase19Task5Detail", "userPhotoManagementVerification"),
      planned("phase19Task6", "phase19Task6Detail"),
    ],
  },
  {
    number: "20", title: "phase20Title", description: "phase20Description",
    tasks: [
      done("phase20Task1", "phase20Task1Detail", "phase20Verification"),
      done("phase20Task2", "phase20Task2Detail", "phase20Verification"),
      done("phase20Task3", "phase20Task3Detail", "phase20Verification"),
      done("phase20Task4", "phase20Task4Detail", "phase20Verification"),
      done("phase20Task5", "phase20Task5Detail", "phase20Verification"),
      planned("phase20Task6", "phase20Task6Detail"),
      planned("phase20Task7", "phase20Task7Detail"),
    ],
  },
  {
    number: "21", title: "phase21Title", description: "phase21Description",
    tasks: [
      done("phase21Task0", "phase21Task0Detail", "phase21Task0Verification"),
      done("phase21Task1", "phase21Task1Detail", "phase21Task1Verification"),
      done("phase21Task2", "phase21Task2Detail", "phase21Task2Verification"),
      done("phase21Task3", "phase21Task3Detail", "phase21Task3Verification"),
      done("phase21Task4", "phase21Task4Detail", "phase21Task4Verification"),
      done("phase21Task5", "phase21Task5Detail", "phase21Task5Verification"),
      { ...planned("phase21Task6", "phase21Task6Detail"), verification: "phase21Task6Verification" },
      done("phase21Task7", "phase21Task7Detail", "phase21Task7Verification"),
      done("phase21Task8", "phase21Task8Detail", "phase21Task8Verification"),
      done("phase21Task9", "phase21Task9Detail", "phase21Task9Verification"),
      { ...planned("phase21Task10", "phase21Task10Detail"), verification: "phase21Task10Verification" },
      done("phase21Task11", "phase21Task11Detail", "phase21Task11Verification"),
      planned("phase21Task12", "phase21Task12Detail"),
    ],
  },
  {
    number: "OTOPARK-05", title: "parkingPhase05Title", description: "parkingPhase05Description",
    tasks: [
      done("parkingPhase05Operations", "parkingPhase05OperationsDetail", "parkingPhase05Verification"),
      done("parkingPhase05Acceptance", "parkingPhase05AcceptanceDetail", "parkingPhase05Verification"),
    ],
  },
  {
    number: "OTOPARK-06", title: "parkingPhase06Title", description: "parkingPhase06Description",
    tasks: [
      done("parkingPhase06Delivery", "parkingPhase06DeliveryDetail", "parkingPhase06Verification"),
      done("parkingPhase06Acceptance", "parkingPhase06AcceptanceDetail", "parkingPhase06Verification"),
    ],
  },
  {
    number: "OTOPARK-07", title: "parkingPhase07Title", description: "parkingPhase07Description",
    tasks: [
      done("parkingPhase07Delivery", "parkingPhase07DeliveryDetail", "parkingPhase07Verification"),
      done("parkingPhase07Acceptance", "parkingPhase07AcceptanceDetail", "parkingPhase07Verification"),
    ],
  },
  {
    number: "OTOPARK-08", title: "parkingPhase08Title", description: "parkingPhase08Description",
    tasks: [
      done("parkingPhase08Verification", "parkingPhase08VerificationDetail", "parkingPhase08Evidence"),
      planned("parkingPhase08Release", "parkingPhase08ReleaseDetail"),
    ],
  },
];
