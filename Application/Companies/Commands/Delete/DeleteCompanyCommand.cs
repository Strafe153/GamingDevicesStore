﻿using Application.Abstractions.MediatR;
using Domain.Entities;

namespace Application.Companies.Commands.Delete;

public sealed record DeleteCompanyCommand(Company Company) : ICommand;
