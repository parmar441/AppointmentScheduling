using AutoMapper;
using ClinicManagement.Core;
using ClinicManagement.Core.Dto;
using ClinicManagement.Core.Models;
using ClinicManagement.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace ClinicManagement.Controllers.Api
{
   // [ApiController]
   //[Route("api/[controller]")]
    public class PatientsController : ApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        public PatientsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

        }

        public IHttpActionResult GetPatients()
        {
            var patientsQuery = _unitOfWork.Patients.GetPatients();


            var patientDto = patientsQuery.ToList()
                                          .Select(Mapper.Map<Patient, PatientDto>);

            return Ok(patientDto);

        }


        [HttpDelete]
        public IHttpActionResult Delete(int id)
        {
            var patient = _unitOfWork.Patients.GetPatient(id);
            _unitOfWork.Patients.Remove(patient);
            _unitOfWork.Complete();
            return Ok();
        }      

        [Route("~/api/Delete/")]
        [HttpDelete]
        public IHttpActionResult DeleteData([FromBody] int id)
        {
            var patient = _unitOfWork.Patients.GetPatient(id);
            _unitOfWork.Patients.Remove(patient);
            _unitOfWork.Complete();
            return Ok();
        }

        [Route("~/api/GetAppointmentsByPatientId/")]
        [HttpGet]
        public IEnumerable<Appointment> Details(int id)
        {
            var appointment = _unitOfWork.Appointments.GetAppointmentWithPatient(id);
            return appointment;
        }

        [Route("~/api/GetAllAppointments/")]
        [HttpGet]
        public IEnumerable<Appointment> Index()
        {
            var appointments = _unitOfWork.Appointments.GetAppointments();
            return appointments;
        }

        [Route("~/api/GetAppointmentsByDoctorId/")]
        [HttpGet]
        public IEnumerable<Appointment> GetAppointmentByDoctor(int id)
        {
            var appointment = _unitOfWork.Appointments.GetAppointmentByDoctor(id);
            return appointment;
        }

        [Route("~/api/GetAppointmentsByDate/")]
        [HttpGet]
        public IEnumerable<Appointment> GetAppointmentsByDate(DateTime date)
        {
            var appointment = _unitOfWork.Appointments.GetDaillyAppointments(date);
            return appointment;
        }

        [Route("~/api/GetAllDoctors/")]
        [HttpGet]
        public IEnumerable<Doctor> GetAllDoctors()
        {
            var doctors = _unitOfWork.Doctors.GetAllDectors();
            return doctors;
        }

        [Route("~/api/GetDoctorByDoctorId/")]
        [HttpGet]
        public Doctor GetDoctorByDoctorId(int id)
        {
            var doctor = _unitOfWork.Doctors.GetDoctorById(id);
            return doctor;
        }
        [Route("~/api/GetAllDoctorByDepartmentId/")]
        [HttpGet]
        public IEnumerable<Doctor> GetAllDoctorByDepartmentId(int id)
        {
            var doctor = _unitOfWork.Doctors.GetAllDoctorByDepartmentId(id);
            return doctor;
        }

        [HttpPost]
        [Route("~/api/EditDoctor/")]
        public bool Edit([FromUri]DoctorFormViewModel viewModel)
        {
            try
            {
                var doctorInDb = _unitOfWork.Doctors.GetDoctor(viewModel.Id);
                doctorInDb.Id = viewModel.Id;
                doctorInDb.Name = viewModel.Name;
                doctorInDb.Phone = viewModel.Phone;
                doctorInDb.Address = viewModel.Address;
                doctorInDb.IsAvailable = viewModel.IsAvailable;
                doctorInDb.SpecializationId = viewModel.Specialization;

                _unitOfWork.Complete();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [Route("~/api/GetMissedAppointments/")]
        [HttpGet]
        public IEnumerable<Appointment> GetMissedAppointments()
        {
            var appointments = _unitOfWork.Appointments.GetMissedAppointments();
            if (appointments.Any())
                return null;
            return appointments;
        }

        [Route("~/api/CreateAppointments/")]
        [HttpPost]
        public Appointment Create([FromUri]AppointmentFormViewModel viewModel)
        {
            try
            {
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
                {
                    return new Appointment { Detail = "Appointment slot is already booked" };
                }

                _unitOfWork.Appointments.Add(appointment);
                _unitOfWork.Complete();

                return appointment;
            }
            catch (Exception ex)
            {
                return new Appointment { Detail = ex.InnerException.InnerException.Message }; ;
            }
        }
    }
}
