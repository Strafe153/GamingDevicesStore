﻿using Microsoft.AspNetCore.Mvc;
using GamingDevicesStore.Models;
using GamingDevicesStore.Dtos.Company;
using GamingDevicesStore.Repositories.Interfaces;
using AutoMapper;

namespace GamingDevicesStore.Controllers
{
    [Route("api/companies")]
    [ApiController]
    public class CompaniesController : ControllerBase
    {
        private readonly IControllable<Company> _repo;
        private readonly IMapper _mapper;

        public CompaniesController(IControllable<Company> repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompanyReadDto>>> GetAllCompaniesAsync()
        {
            IEnumerable<Company> companies = await _repo.GetAllAsync();
            var readDtos = _mapper.Map<IEnumerable<CompanyReadDto>>(companies);

            return Ok(readDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CompanyReadDto>> GetCompanyAsync(Guid id)
        {
            Company? company = await _repo.GetByIdAsync(id);

            if (company is null)
            {
                return NotFound("Company not found");
            }

            var readDto = _mapper.Map<CompanyReadDto>(company);

            return Ok(readDto);
        }

        [HttpPost]
        public async Task<ActionResult<CompanyReadDto>> CreateCompanyAsync(CompanyCreateUpdateDto createDto)
        {
            var company = _mapper.Map<Company>(createDto);

            _repo.Add(company);
            await _repo.SaveChangesAsync();

            var readDto = _mapper.Map<CompanyReadDto>(company);

            return CreatedAtAction(nameof(GetAllCompaniesAsync), new { id = readDto.Id }, readDto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateCompanyAsync(Guid id, CompanyCreateUpdateDto updateDto)
        {
            Company? company = await _repo.GetByIdAsync(id);

            if (company is null)
            {
                return NotFound("Company not found");
            }

            _mapper.Map(updateDto, company);
            _repo.Update(company);
            await _repo.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCompanyAsync(Guid id)
        {
            Company? company = await _repo.GetByIdAsync(id);

            if (company is null)
            {
                return NotFound("Company not found");
            }

            _repo.Remove(company);
            await _repo.SaveChangesAsync();

            return NoContent();
        }
    }
}
