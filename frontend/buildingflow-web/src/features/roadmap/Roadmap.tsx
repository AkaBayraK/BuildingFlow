"use client";
import {useLocale,useTranslations} from "next-intl";
import { milestones } from "./milestones";

export function Roadmap() {
  const t=useTranslations("ui"); const m=useTranslations("management"); const locale=useLocale();

  const total = milestones.reduce((count, milestone) => count + milestone.tasks.length, 0);
  const completed = milestones.reduce((count, milestone) => count + milestone.tasks.filter(task => task.done).length, 0);

  const nextStep = milestones.find(milestone => milestone.tasks.some(task => !task.done))?.number;

  return (
    <section className="roadmap" aria-labelledby="roadmap-title">
      <div className="roadmap-heading">
        <div>
          <span className="eyebrow">{t("buildingflowSTEPBYSTEP")}</span>
          <h2 id="roadmap-title">{t("developmentRoadmap")}</h2>
          <p>{t("completedTasksHaveACheckmarkTasksAreCheckedOnly")}</p>
        </div>
        <span className="badge">{t("roadmapProgress",{completed,total})}</span>
      </div>
      <p className="roadmap-note">{t("thisListShowsDevelopmentProgressLiveServiceHealthAppears")}</p>
      <div className="milestones">
        <details className="milestone milestone-done">
          <summary><span className="milestone-number text-base">UI 06</span><span className="milestone-title"><strong>{m("stage06Title")}</strong><span className="milestone-description">{m("stage06Description")}</span></span><span className="milestone-state done"><strong>{t("completed")}</strong></span><span className="milestone-chevron" aria-hidden="true">⌄</span></summary>
          <ul className="roadmap-tasks">
            <li className="task-done"><span className="task-marker" aria-hidden="true">✓</span><div><h3><strong>{m("stage06Units")}</strong></h3><p>{m("stage06UnitsDetail")}</p><p className="task-verification"><strong>{m("stage06UnitEvidence")}</strong></p></div></li>
            <li className="task-done"><span className="task-marker" aria-hidden="true">✓</span><div><h3><strong>{m("stage06People")}</strong></h3><p>{m("stage06PeopleDetail")}</p><p className="task-verification"><strong>{m("stage06PeopleEvidence")}</strong></p></div></li>
            <li className="task-done"><span className="task-marker" aria-hidden="true">✓</span><div><h3><strong>{m("stage06Checks")}</strong></h3><p className="task-verification"><strong>{m("stage06ChecksEvidence")}</strong></p></div></li>
            <li className="task-done"><span className="task-marker" aria-hidden="true">✓</span><div><h3><strong>{m("stage06Panels")}</strong></h3><p className="task-verification"><strong>{m("stage06PanelEvidence")}</strong></p></div></li>
          </ul>
        </details>
        {milestones.map(milestone => {
          const count = milestone.tasks.filter(task => task.done).length;
          const finished = count === milestone.tasks.length;
          return (
            <details key={milestone.number} className={`milestone ${finished ? "milestone-done" : ""}`}>
              <summary>
                <span className="milestone-number">{milestone.number}</span>
                <span className="milestone-title">{t(milestone.title)}<span className="milestone-description">{t(milestone.description)}</span></span>
                <span className={`milestone-state ${finished ? "done" : ""}`}>{finished ? t("completed") : milestone.number === nextStep ? t("nextStep") : t("planned")}<span>{count} / {milestone.tasks.length}</span></span>
                <span className="milestone-chevron" aria-hidden="true">⌄</span>
              </summary>
              <ul className="roadmap-tasks">
                {milestone.tasks.map(task => (
                  <li key={task.title} className={task.done ? "task-done" : ""}>
                    <span className="task-marker" aria-hidden="true">{task.done ? "✓" : "○"}</span>
                    <div><h3>{task.done ? <strong>{t(task.title)}</strong> : t(task.title)}</h3><p>{t(task.detail)}</p>{task.verification && <p className="task-verification"><strong>{t("testVerification")} {t(task.verification)}</strong></p>}</div>
                    <span className="task-state">{task.done ? t("completed2") : t("pending")}</span>
                  </li>
                ))}
              </ul>
            </details>
          );
        })}
      </div>
    </section>
  );
}
