﻿using AutoMapper;
using ClinicManagement.Core;
using ClinicManagement.Core.Dto;
using ClinicManagement.Core.Models;
using System.Linq;
using System.Web.Http;

namespace ClinicManagement.Controllers.Api
{
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

        [Route("~/api/GetAppointments/")]
        [HttpPost]
        public Appointment Details(int id)
        {
            var appointment = _unitOfWork.Appointments.GetAppointmentWithPatient(id);
            return Mapper.Map<Appointment>(appointment);
        }

        [Route("~/api/GetAllAppointments/")]
        [HttpGet]
        public Appointment Index()
        {
            var appointments = _unitOfWork.Appointments.GetAppointments();
            return Mapper.Map<Appointment>(appointments);
        }
    }
}
