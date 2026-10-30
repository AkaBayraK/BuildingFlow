import test from 'node:test';import assert from 'node:assert/strict';
import {readSchedule,readOccurrence,readPreview,readHoliday,readScheduleMutation,readOccurrenceMutation,readMaintenanceHistory,invalidScheduleField,scheduleBody,scheduleForm} from '../src/features/suppliers/maintenance-model.ts';
const tenant='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',supplier='bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',id='cccccccc-cccc-4ccc-8ccc-cccccccccccc',property='dddddddd-dddd-4ddd-8ddd-dddddddddddd',stamp='2026-10-30T09:00:00Z';
const schedule={id,tenantId:tenant,supplierId:supplier,propertyId:property,propertyName:'Area',areaServiceId:id,serviceName:'Inspection',contractId:null,contractTitle:null,title:'Elevator inspection',description:'Monthly\ncheck',type:'INSPECTION',frequency:'MONTHLY',startDate:'2027-01-04',endDate:'2027-06-30',reminderDays:7,nonWorkingDayRule:'NEXT_WORKING_DAY',status:'ACTIVE',endReason:null,plannedCount:6,completedCount:0,skippedCount:0,overdueCount:1,version:1,createdAt:stamp};
const occurrence={id,scheduleId:id,propertyId:property,propertyName:'Area',scheduleTitle:'Elevator inspection',sequence:1,nominalDate:'2027-01-02',plannedDate:'2027-01-04',maintenancePlanId:property,status:'PLANNED',isOverdue:false,isReminderDue:true,completedOn:null,delayDays:null,completionNote:null,workOrderId:null,skipReason:null,version:1};
const parent={id:supplier,tenantId:tenant,name:'Supplier',kind:'COMPANY',description:null,status:'ACTIVE',statusReason:null,isEnabled:true,version:3,createdAt:stamp,modifiedAt:stamp};
test('schedule DTO keeps tenant, supplier, period, contract pairing and ended-state invariants',()=>{
 assert.deepEqual(readSchedule(schedule,tenant,supplier),schedule);
 for(const patch of [{tenantId:property},{supplierId:property},{frequency:'DAILY'},{nonWorkingDayRule:'IGNORE'},{endDate:'2026-12-31'},{endDate:'2029-02-01'},{reminderDays:61},{contractId:id},{contractTitle:'Orphan title'},{status:'ENDED'},{status:'ENDED',endReason:'Terminated'},{overdueCount:7},{title:'bad\u0000'},{startDate:'2027-02-30'}])assert.throws(()=>readSchedule({...schedule,...patch},tenant,supplier));
 assert.equal(readSchedule({...schedule,status:'ENDED',endReason:'Terminated',plannedCount:0,overdueCount:0},tenant,supplier).status,'ENDED');
 assert.equal(readSchedule({...schedule,contractId:id,contractTitle:'Annual'},tenant,supplier).contractTitle,'Annual');
});
test('occurrence DTO requires consistent completion evidence, delay and skip reasons',()=>{
 assert.deepEqual(readOccurrence(occurrence,id),occurrence);assert.throws(()=>readOccurrence(occurrence,property));
 const done={...occurrence,status:'COMPLETED',isReminderDue:false,completedOn:'2027-01-08',delayDays:4,completionNote:'Signed report',workOrderId:id};
 assert.equal(readOccurrence(done).delayDays,4);
 for(const patch of [{delayDays:3},{completionNote:null},{completedOn:null},{isOverdue:true},{skipReason:'Also skipped'},{workOrderId:'bad'}])assert.throws(()=>readOccurrence({...done,...patch}));
 for(const patch of [{isOverdue:true,isReminderDue:false},{workOrderId:id},{status:'SKIPPED'},{status:'DONE'},{sequence:0}])assert.throws(()=>readOccurrence({...occurrence,...patch}));
 assert.equal(readOccurrence({...occurrence,status:'SKIPPED',isReminderDue:false,skipReason:'Building closed'}).skipReason,'Building closed');
 assert.equal(readOccurrence({...done,completedOn:'2027-01-01',delayDays:0}).delayDays,0);
});
test('preview rejects inconsistent shift flags, unordered sequences and wrong conflict totals',()=>{
 const dates=[{sequence:1,nominalDate:'2027-01-02',plannedDate:'2027-01-04',shifted:true,conflict:false},{sequence:2,nominalDate:'2027-02-02',plannedDate:'2027-02-02',shifted:false,conflict:true}];
 assert.equal(readPreview({dates,conflictCount:1}).dates.length,2);
 for(const v of [{dates,conflictCount:0},{dates:[{...dates[0],shifted:false},dates[1]],conflictCount:1},{dates:[dates[1],dates[0]],conflictCount:1},{dates:[{...dates[0],plannedDate:'2027-03-10'},dates[1]],conflictCount:1}])assert.throws(()=>readPreview(v));
});
test('mutations keep current version replay and reject foreign supplier or future versions',()=>{
 assert.equal(readScheduleMutation({record:{...schedule,version:4},supplier:parent,replayed:true,appliedVersion:1},tenant,supplier).record.version,4);
 assert.throws(()=>readScheduleMutation({record:schedule,supplier:{...parent,id:property},replayed:false,appliedVersion:1},tenant,supplier));
 assert.throws(()=>readScheduleMutation({record:schedule,supplier:parent,replayed:false,appliedVersion:2},tenant,supplier));
 assert.equal(readOccurrenceMutation({record:{...occurrence,version:2},replayed:false,appliedVersion:2},id).record.version,2);
 assert.throws(()=>readOccurrenceMutation({record:occurrence,replayed:false,appliedVersion:1},property));
});
test('holiday and history DTOs reject foreign tenants and unknown actions',()=>{
 const holiday={id,tenantId:tenant,date:'2027-01-01',name:'New Year',version:1};assert.deepEqual(readHoliday(holiday,tenant),holiday);
 for(const patch of [{tenantId:property},{date:'1999-12-31'},{name:' '}])assert.throws(()=>readHoliday({...holiday,...patch},tenant));
 assert.equal(readMaintenanceHistory({id,action:'SKIP',appliedVersion:2,snapshot:'{}',createdAt:stamp}).action,'SKIP');
 assert.throws(()=>readMaintenanceHistory({id,action:'DELETE',appliedVersion:2,snapshot:'{}',createdAt:stamp}));
});
test('schedule form validation points to the first invalid field and sends trimmed, typed values',()=>{
 const form={...scheduleForm(),title:' Inspection ',startDate:'2027-01-04',endDate:'2027-06-30',contractId:id};
 assert.equal(invalidScheduleField(form),null);
 assert.equal(invalidScheduleField({...form,title:' '}),'title');assert.equal(invalidScheduleField({...form,endDate:'2027-01-01'}),'endDate');assert.equal(invalidScheduleField({...form,endDate:'2029-02-01'}),'endDate');assert.equal(invalidScheduleField({...form,reminderDays:'61'}),'reminderDays');assert.equal(invalidScheduleField({...form,startDate:''}),'startDate');
 assert.deepEqual(scheduleBody({...form,description:'  '},property,id),{propertyId:property,areaServiceId:id,contractId:id,title:'Inspection',description:null,type:'MAINTENANCE',frequency:'MONTHLY',startDate:'2027-01-04',endDate:'2027-06-30',reminderDays:7,nonWorkingDayRule:'NEXT_WORKING_DAY'});
});
