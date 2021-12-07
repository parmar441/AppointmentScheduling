using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Web.Mvc;
using ClinicManagement.Core;
using ClinicManagement.Core.Models;
using ClinicManagement.Core.ViewModel;
using Newtonsoft.Json;

namespace ClinicManagement.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AppointmentsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public ActionResult Index()
        {
            var appointments = _unitOfWork.Appointments.GetAppointments();
            return View(appointments);
        }

        public ActionResult Details(int id)
        {
            var appointment = _unitOfWork.Appointments.GetAppointmentWithPatient(id);
            return View("_AppointmentPartial", appointment);
        }
        //public ActionResult Patients(int id)
        //{
        //    var viewModel = new DoctorDetailViewModel()
        //    {
        //        Appointments = _unitOfWork.Appointments.GetAppointmentByDoctor(id),
        //    };
        //    //var upcomingAppnts = _unitOfWork.Appointments.GetAppointmentByDoctor(id);
        //    return View(viewModel);
        //}

        public ActionResult Create(int id)
        {
            var viewModel = new AppointmentFormViewModel
            {
                Patient = id,
                Doctors = _unitOfWork.Doctors.GetAvailableDoctors(),

                Heading = "New Appointment"
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AppointmentFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel.Doctors = _unitOfWork.Doctors.GetAvailableDoctors();
                return View(viewModel);

            }
            var appointment = new Appointment()
            {
                StartDateTime = viewModel.GetStartDateTime(),
                Detail = viewModel.Detail,
                Status = false,
                PatientId = viewModel.Patient,
                Doctor = _unitOfWork.Doctors.GetDoctor(viewModel.Doctor)

            };
            //Check if the slot is available
            if (_unitOfWork.Appointments.ValidateAppointment(appointment.StartDateTime, viewModel.Doctor))
                return View("InvalidAppointment");
           
            _unitOfWork.Appointments.Add(appointment);
            _unitOfWork.Complete();

            var patient = _unitOfWork.Patients.GetPatient(viewModel.Patient);
            Email("Reminder-Your appointment on ", viewModel.Date, patient.Name, appointment.Doctor.Name);
            postPatientManagementPortal(appointment.Doctor.Id, patient.Id);
            return RedirectToAction("Index", "Appointments");
        }

        public void postPatientManagementPortal(int doctorId, int patientId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var pocoObject = new
                    {
                        doctorId = doctorId.ToString(),
                        patientId = patientId.ToString()
                    };

                    //Converting the object to a json string. NOTE: Make sure the object doesn't contain circular references.
                    string json = JsonConvert.SerializeObject(pocoObject);

                    //Needed to setup the body of the request
                    StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = client.PostAsync("https://dry-ocean-01268.herokuapp.com/practicemanagement", data).Result;
                }
            }
            catch (Exception) { }

         }

        public static void Email(string htmlString, string date, string patientName, string doctorName)
         {
            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress("parmarbhavin1012@gmail.com");
                    mail.To.Add("cg485@njit.edu");
                    mail.Subject = htmlString + date;
                    mail.Body = CreateEmailBody(patientName, doctorName, date);
                    mail.IsBodyHtml = true;
                  //  mail.Attachments.Add(new Attachment("C:\\file.zip"));

                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential("parmarbhavin1012@gmail.com", "Vadodara@123");
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }
                };
            }
            catch (Exception ex) { }
        }

        public static string CreateEmailBody(string patientName, string doctorName, string dateTime)
        {
            string body = "<br>Dear "+ patientName + "," + "<br/>" + "This is a friendly reminder confirming your appointment with" + doctorName + "on "+ dateTime + ".Please try to arrive 15 minutes early and bring your[IMPORTANT - DOCUMENT]." + "<br/><br/>" + "If you have any questions or you need to reschedule, please call our office at (732)318-1413. Otherwise, we look forward to seeing you on "+ dateTime + ".Have a wonderful day! " + "<br/><br/>" + "Warm regards," + "<br/>" + "CS 673";
            return body;
        }
        public ActionResult Edit(int id)
        {
            var appointment = _unitOfWork.Appointments.GetAppointment(id);
            var viewModel = new AppointmentFormViewModel()
            {
                Heading = "New Appointment",
                Id = appointment.Id,
                Date = appointment.StartDateTime.ToString("dd/MM/yyyy"),
                Time = appointment.StartDateTime.ToString("HH:mm"),
                Detail = appointment.Detail,
                Status = appointment.Status,
                Patient = appointment.PatientId,
                Doctor = appointment.DoctorId,
                //Patients = _unitOfWork.Patients.GetPatients(),
                Doctors = _unitOfWork.Doctors.GetDectors()
            };
            return View(viewModel);
        }

        public ActionResult Remove(int id)
        {
            var appointment = _unitOfWork.Appointments.GetAppointment(id);
            var doctor = _unitOfWork.Doctors.GetDoctorById(appointment.DoctorId);
            var patient = _unitOfWork.Patients.GetPatient(appointment.PatientId);
            _unitOfWork.Appointments.Remove(appointment);
            EmailDelete("Your appointment on ", appointment.StartDateTime.ToString(), patient.Name, doctor.Name);

            return RedirectToAction("Index", "Appointments");
        }

        public static void EmailDelete(string htmlString, string date, string patientName, string doctorName)
        {
            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress("parmarbhavin1012@gmail.com");
                    mail.To.Add("cg485@njit.edu");
                    mail.Subject = htmlString + date + " Deleted";
                    mail.Body = "<br>Dear " + patientName + "," + "<br/>" + "You have cancelled your appointment with " + doctorName + " on " + date + "..Have a wonderful day! " + "<br/><br/>" + "Warm regards," + "<br/>" + "CS 673"; 
                    mail.IsBodyHtml = true;

                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential("parmarbhavin1012@gmail.com", "Vadodara@123");
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }
                };
            }
            catch (Exception ex) { }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(AppointmentFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel.Doctors = _unitOfWork.Doctors.GetDectors();
                viewModel.Patients = _unitOfWork.Patients.GetPatients();
                return View(viewModel);
            }

            var appointmentInDb = _unitOfWork.Appointments.GetAppointment(viewModel.Id);
            appointmentInDb.Id = viewModel.Id;
            appointmentInDb.StartDateTime = viewModel.GetStartDateTime();
            appointmentInDb.Detail = viewModel.Detail;
            appointmentInDb.Status = viewModel.Status;
            appointmentInDb.PatientId = viewModel.Patient;
            appointmentInDb.DoctorId = viewModel.Doctor;
            if (_unitOfWork.Appointments.ValidateAppointment(appointmentInDb.StartDateTime, viewModel.Doctor))
                return View("InvalidAppointment");

            _unitOfWork.Complete();

            var patient = _unitOfWork.Patients.GetPatient(appointmentInDb.PatientId);
            var doctor = _unitOfWork.Doctors.GetDoctorById(appointmentInDb.DoctorId);
            EmailEdit("Appointment Edited on ", appointmentInDb.StartDateTime.ToString(), patient.Name, doctor.Name);
            return RedirectToAction("Index");

        }

        public static void EmailEdit(string htmlString, string date, string patientName, string doctorName)
        {
            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress("parmarbhavin1012@gmail.com");
                    mail.To.Add("cg485@njit.edu");
                    mail.Subject = htmlString + date;
                    mail.Body = "<br>Dear " + patientName + "," + "<br/>" + "This is a friendly reminder confirming your appointment with" + doctorName + "on " + date + ".Please try to arrive 15 minutes early and bring your[IMPORTANT - DOCUMENT]." + "<br/><br/>" + "If you have any questions or you need to reschedule, please call our office at (732)318-1413. Otherwise, we look forward to seeing you on " + date + ".Have a wonderful day! " + "<br/><br/>" + "Warm regards," + "<br/>" + "CS 673"; ;
                    mail.IsBodyHtml = true;
                    //  mail.Attachments.Add(new Attachment("C:\\file.zip"));

                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential("parmarbhavin1012@gmail.com", "Vadodara@123");
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }
                };
            }
            catch (Exception ex) { }
        }
        public ActionResult DoctorsList()
        {
            var doctors = _unitOfWork.Doctors.GetAvailableDoctors();
            if (HttpContext.Request.IsAjaxRequest())
                return Json(new SelectList(doctors.ToArray(), "Id", "Name"), JsonRequestBehavior.AllowGet);
            return RedirectToAction("Create");
        }

        public PartialViewResult GetUpcommingAppointments(int id)
        {
            var appointments = _unitOfWork.Appointments.GetTodaysAppointments(id);
            return PartialView(appointments);
        }

    }
}