package com.buildingflow.identity;

import org.keycloak.Config;
import org.keycloak.email.EmailTemplateProvider;
import org.keycloak.events.Details;
import org.keycloak.events.Event;
import org.keycloak.events.EventListenerProvider;
import org.keycloak.events.EventListenerProviderFactory;
import org.keycloak.events.EventType;
import org.keycloak.events.admin.AdminEvent;
import org.keycloak.models.KeycloakSession;
import org.keycloak.models.KeycloakSessionFactory;
import org.keycloak.models.RealmModel;
import org.keycloak.models.UserModel;
import org.keycloak.models.utils.KeycloakModelUtils;
import org.keycloak.models.AbstractKeycloakTransaction;
import java.util.logging.Logger;

/** Project-owned listener; no tokens, old/new addresses or exceptions enter logs. */
public final class ContactNoticeFactory implements EventListenerProviderFactory {
    private static final Logger LOG = Logger.getLogger(ContactNoticeFactory.class.getName());
    public String getId() { return "buildingflow-contact-notice"; }
    public void init(Config.Scope config) { }
    public void postInit(KeycloakSessionFactory factory) { }
    public void close() { }

    public EventListenerProvider create(KeycloakSession session) {
        return new EventListenerProvider() {
            public void close() { }
            public void onEvent(AdminEvent event, boolean includeRepresentation) { }
            public void onEvent(Event event) {
                if (event.getType() != EventType.UPDATE_EMAIL || event.getError() != null || event.getDetails() == null) return;
                String oldEmail = event.getDetails().get(Details.PREVIOUS_EMAIL);
                String newEmail = event.getDetails().get(Details.UPDATED_EMAIL);
                if (oldEmail == null || oldEmail.isBlank() || oldEmail.equalsIgnoreCase(newEmail)) return;
                String realmId = event.getRealmId(), userId = event.getUserId();
                KeycloakSessionFactory factory = session.getKeycloakSessionFactory();
                org.keycloak.http.HttpRequest request = session.getContext().getHttpRequest();
                // Notify only a committed change; rollbacks must never produce a false security notice.
                session.getTransactionManager().enlistAfterCompletion(new AbstractKeycloakTransaction() {
                    protected void rollbackImpl() { }
                    protected void commitImpl() {
                        try {
                            KeycloakModelUtils.runJobInTransaction(factory, fresh -> {
                                RealmModel realm = fresh.realms().getRealm(realmId);
                                fresh.getContext().setRealm(realm);
                                UserModel user = fresh.users().getUserById(realm, userId);
                                if (user == null) return;
                                fresh.getContext().setHttpRequest(request);
                                try {
                                    fresh.getProvider(EmailTemplateProvider.class).setRealm(realm).setUser(user)
                                        .send("buildingflowContactChangedSubject", "buildingflow-contact-changed.ftl", new java.util.HashMap<>(), oldEmail);
                                } catch (Exception ignored) { LOG.severe("BF_CONTACT_NOTICE_UNAVAILABLE"); }
                            });
                        } catch (Exception ignored) { LOG.severe("BF_CONTACT_NOTICE_UNAVAILABLE"); }
                    }
                });
            }
        };
    }
}
