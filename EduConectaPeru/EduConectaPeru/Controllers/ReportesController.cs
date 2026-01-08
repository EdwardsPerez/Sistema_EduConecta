using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduConectaPeru.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EduConectaPeru.Controllers
{
    [Authorize(Policy = "AdminOrSecretaria")]
    public class ReportesController : Controller
    {
        private readonly PaymentRepositoryADO _paymentRepo;
        private readonly QuotaRepositoryADO _quotaRepo;
        private readonly StudentRepositoryADO _studentRepo;
        private readonly MatriculaRepositoryADO _matriculaRepo;
        private readonly GradoSeccionRepositoryADO _gradoSeccionRepo;

        public ReportesController(
            PaymentRepositoryADO paymentRepo,
            QuotaRepositoryADO quotaRepo,
            StudentRepositoryADO studentRepo,
            MatriculaRepositoryADO matriculaRepo,
            GradoSeccionRepositoryADO gradoSeccionRepo)
        {
            _paymentRepo = paymentRepo;
            _quotaRepo = quotaRepo;
            _studentRepo = studentRepo;
            _matriculaRepo = matriculaRepo;
            _gradoSeccionRepo = gradoSeccionRepo;
        }

        // GET: Reportes
        public IActionResult Index()
        {
            return View();
        }

        // REPORTE 1: Comprobante de Pago Individual
        public async Task<IActionResult> ComprobantePago(int paymentId)
        {
            var payment = await _paymentRepo.ObtenerPagoPorIdAsync(paymentId);
            if (payment == null)
            {
                return NotFound();
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(container =>
                    {
                        container.Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("EduConecta Perú").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text("Sistema de Gestión Educativa").FontSize(10);
                                column.Item().Text("RUC: 20123456789").FontSize(9);
                            });

                            row.RelativeItem().Column(column =>
                            {
                                column.Item().AlignRight().Text("COMPROBANTE DE PAGO").FontSize(16).Bold();
                                column.Item().AlignRight().Text($"N° {payment.PaymentId:D8}").FontSize(12);
                                column.Item().AlignRight().Text($"Fecha: {payment.FechaPago:dd/MM/yyyy}").FontSize(9);
                            });
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(info =>
                        {
                            info.Item().Text("INFORMACIÓN DEL ESTUDIANTE").FontSize(12).Bold();
                            info.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text($"Estudiante: {payment.Student?.NombreCompleto}");
                                row.RelativeItem().Text($"DNI: {payment.Student?.DNI}");
                            });
                        });

                        column.Item().PaddingTop(10);

                        column.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(detalle =>
                        {
                            detalle.Item().Text("DETALLE DEL PAGO").FontSize(12).Bold();
                            detalle.Item().PaddingTop(5).Text($"Concepto: Cuota {payment.Quota?.Mes} {payment.Quota?.Anio}");
                            detalle.Item().Text($"Método de Pago: {payment.PaymentType?.TypeName}");
                            if (payment.Bank != null)
                            {
                                detalle.Item().Text($"Banco: {payment.Bank.BankName}");
                            }
                            if (!string.IsNullOrEmpty(payment.NumeroOperacion))
                            {
                                detalle.Item().Text($"N° Operación: {payment.NumeroOperacion}");
                            }
                        });

                        column.Item().PaddingTop(10);

                        column.Item().Border(1).BorderColor(Colors.Grey.Medium).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL PAGADO:").FontSize(14).Bold();
                            row.RelativeItem().AlignRight().Text($"S/ {payment.Monto:N2}").FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                        });

                        column.Item().PaddingTop(20).Text("Observaciones:").FontSize(10).Bold();
                        column.Item().Text(payment.Observaciones ?? "Ninguna").FontSize(9);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Documento generado el ").FontSize(8);
                        text.Span($"{DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).Bold();
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Comprobante_{payment.PaymentId}.pdf");
        }

        // REPORTE 2: Estudiantes Deudores
        public async Task<IActionResult> EstudiantesDeudores()
        {
            var todasCuotas = await _quotaRepo.ObtenerCuotasAsync();
            var cuotasPendientes = todasCuotas
                .Where(c => c.IsActive && c.PaymentStatus?.StatusName != "Pagado")
                .GroupBy(c => c.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    Student = g.First().Student,
                    CantidadCuotas = g.Count(),
                    TotalDeuda = g.Sum(c => c.Monto)
                })
                .OrderByDescending(x => x.TotalDeuda)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(container =>
                    {
                        container.Column(column =>
                        {
                            column.Item().Text("EduConecta Perú").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                            column.Item().Text("REPORTE DE ESTUDIANTES DEUDORES").FontSize(14).Bold();
                            column.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9);
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        column.Item().Text($"Total de estudiantes con deuda: {cuotasPendientes.Count}").FontSize(11).Bold();
                        column.Item().PaddingTop(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("#").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Estudiante").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("DNI").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignCenter().Text("Cuotas").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Deuda Total").FontColor(Colors.White).Bold();
                            });

                            int contador = 1;
                            foreach (var item in cuotasPendientes)
                            {
                                var bgColor = contador % 2 == 0 ? Colors.Grey.Lighten3 : Colors.White;

                                table.Cell().Background(bgColor).Padding(5).Text(contador.ToString());
                                table.Cell().Background(bgColor).Padding(5).Text(item.Student?.NombreCompleto ?? "N/A");
                                table.Cell().Background(bgColor).Padding(5).Text(item.Student?.DNI ?? "N/A");
                                table.Cell().Background(bgColor).Padding(5).AlignCenter().Text(item.CantidadCuotas.ToString());
                                table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"S/ {item.TotalDeuda:N2}").FontColor(Colors.Red.Darken1);

                                contador++;
                            }

                            table.Cell().ColumnSpan(4).Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("TOTAL:").Bold();
                            table.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text($"S/ {cuotasPendientes.Sum(x => x.TotalDeuda):N2}").Bold().FontColor(Colors.Red.Darken2);
                        });
                    });

                    page.Footer().AlignCenter().Text($"Página {{p}} de {{pages}}").FontSize(8);
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Estudiantes_Deudores_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // REPORTE 3: Cuotas por Vencer (próximos 7 días)
        public async Task<IActionResult> CuotasPorVencer()
        {
            var todasCuotas = await _quotaRepo.ObtenerCuotasAsync();
            var fechaLimite = DateTime.Today.AddDays(7);

            var cuotasPorVencer = todasCuotas
                .Where(c => c.IsActive &&
                           c.PaymentStatus?.StatusName != "Pagado" &&
                           c.FechaVencimiento >= DateTime.Today &&
                           c.FechaVencimiento <= fechaLimite)
                .OrderBy(c => c.FechaVencimiento)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(container =>
                    {
                        container.Column(column =>
                        {
                            column.Item().Text("EduConecta Perú").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                            column.Item().Text("REPORTE DE CUOTAS POR VENCER (PRÓXIMOS 7 DÍAS)").FontSize(14).Bold();
                            column.Item().Text($"Del {DateTime.Today:dd/MM/yyyy} al {fechaLimite:dd/MM/yyyy}").FontSize(10);
                            column.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        column.Item().Text($"Total de cuotas por vencer: {cuotasPorVencer.Count}").FontSize(11).Bold();
                        column.Item().PaddingTop(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(25);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Orange.Darken2).Padding(5).Text("#").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Orange.Darken2).Padding(5).Text("Estudiante").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Orange.Darken2).Padding(5).Text("DNI").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Orange.Darken2).Padding(5).Text("Apoderado").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Orange.Darken2).Padding(5).Text("Período").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Orange.Darken2).Padding(5).Text("Vencimiento").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Orange.Darken2).Padding(5).AlignRight().Text("Monto").FontColor(Colors.White).Bold();
                            });

                            int contador = 1;
                            foreach (var cuota in cuotasPorVencer)
                            {
                                var diasRestantes = (cuota.FechaVencimiento - DateTime.Today).Days;
                                var bgColor = contador % 2 == 0 ? Colors.Grey.Lighten3 : Colors.White;

                                table.Cell().Background(bgColor).Padding(3).Text(contador.ToString());
                                table.Cell().Background(bgColor).Padding(3).Text(cuota.Student?.NombreCompleto ?? "N/A");
                                table.Cell().Background(bgColor).Padding(3).Text(cuota.Student?.DNI ?? "N/A");
                                table.Cell().Background(bgColor).Padding(3).Text(cuota.Student?.LegalGuardian?.NombreCompleto ?? "N/A");
                                table.Cell().Background(bgColor).Padding(3).Text($"{cuota.Mes} {cuota.Anio}");
                                table.Cell().Background(bgColor).Padding(3).Column(col =>
                                {
                                    col.Item().Text(cuota.FechaVencimiento.ToString("dd/MM/yyyy"));
                                    col.Item().Text($"({diasRestantes}d)").FontSize(7).FontColor(Colors.Orange.Darken1);
                                });
                                table.Cell().Background(bgColor).Padding(3).AlignRight().Text($"S/ {cuota.Monto:N2}");

                                contador++;
                            }

                            table.Cell().ColumnSpan(6).Background(Colors.Grey.Medium).Padding(5).AlignRight().Text("TOTAL:").Bold();
                            table.Cell().Background(Colors.Grey.Medium).Padding(5).AlignRight().Text($"S/ {cuotasPorVencer.Sum(c => c.Monto):N2}").Bold();
                        });
                    });

                    page.Footer().AlignCenter().Text($"Página {{p}} de {{pages}}").FontSize(8);
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Cuotas_Por_Vencer_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // REPORTE 4: Cuotas Vencidas (Morosos)
        public async Task<IActionResult> CuotasVencidas()
        {
            var todasCuotas = await _quotaRepo.ObtenerCuotasAsync();

            var cuotasVencidas = todasCuotas
                .Where(c => c.IsActive &&
                           c.PaymentStatus?.StatusName != "Pagado" &&
                           c.FechaVencimiento < DateTime.Today)
                .OrderBy(c => c.FechaVencimiento)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(container =>
                    {
                        container.Column(column =>
                        {
                            column.Item().Text("EduConecta Perú").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                            column.Item().Text("REPORTE DE CUOTAS VENCIDAS - ESTUDIANTES MOROSOS").FontSize(14).Bold().FontColor(Colors.Red.Darken2);
                            column.Item().Text($"Fecha de corte: {DateTime.Today:dd/MM/yyyy}").FontSize(10);
                            column.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        column.Item().Background(Colors.Red.Lighten4).Padding(10).Text($"TOTAL DE CUOTAS VENCIDAS: {cuotasVencidas.Count}").FontSize(11).Bold().FontColor(Colors.Red.Darken3);
                        column.Item().PaddingTop(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(25);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(0.8f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Red.Darken2).Padding(5).Text("#").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Red.Darken2).Padding(5).Text("Estudiante").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Red.Darken2).Padding(5).Text("DNI").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Red.Darken2).Padding(5).Text("Apoderado").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Red.Darken2).Padding(5).Text("Teléfono").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Red.Darken2).Padding(5).Text("Período").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Red.Darken2).Padding(5).Text("Vencimiento").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Red.Darken2).Padding(5).AlignRight().Text("Monto").FontColor(Colors.White).Bold();
                            });

                            int contador = 1;
                            foreach (var cuota in cuotasVencidas)
                            {
                                var diasVencidos = (DateTime.Today - cuota.FechaVencimiento).Days;
                                var bgColor = contador % 2 == 0 ? Colors.Red.Lighten5 : Colors.White;

                                table.Cell().Background(bgColor).Padding(3).Text(contador.ToString());
                                table.Cell().Background(bgColor).Padding(3).Text(cuota.Student?.NombreCompleto ?? "N/A");
                                table.Cell().Background(bgColor).Padding(3).Text(cuota.Student?.DNI ?? "N/A");
                                table.Cell().Background(bgColor).Padding(3).Text(cuota.Student?.LegalGuardian?.NombreCompleto ?? "N/A");
                                table.Cell().Background(bgColor).Padding(3).Text(cuota.Student?.LegalGuardian?.Telefono ?? "N/A");
                                table.Cell().Background(bgColor).Padding(3).Text($"{cuota.Mes} {cuota.Anio}");
                                table.Cell().Background(bgColor).Padding(3).Column(col =>
                                {
                                    col.Item().Text(cuota.FechaVencimiento.ToString("dd/MM/yyyy"));
                                    col.Item().Text($"{diasVencidos} días").FontSize(7).Bold().FontColor(Colors.Red.Darken2);
                                });
                                table.Cell().Background(bgColor).Padding(3).AlignRight().Text($"S/ {cuota.Monto:N2}").FontColor(Colors.Red.Darken1);

                                contador++;
                            }

                            table.Cell().ColumnSpan(7).Background(Colors.Red.Darken3).Padding(5).AlignRight().Text("TOTAL MORA:").Bold().FontColor(Colors.White);
                            table.Cell().Background(Colors.Red.Darken3).Padding(5).AlignRight().Text($"S/ {cuotasVencidas.Sum(c => c.Monto):N2}").Bold().FontColor(Colors.White);
                        });
                    });

                    page.Footer().AlignCenter().Text($"Página {{p}} de {{pages}}").FontSize(8);
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Cuotas_Vencidas_Morosos_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // REPORTE 5: Estudiantes por Grado/Sección
        public async Task<IActionResult> EstudiantesPorGrado(int? gradoSeccionId)
        {
            var todasMatriculas = await _matriculaRepo.ObtenerMatriculasAsync();
            var gradoSeccion = gradoSeccionId.HasValue
                ? await _gradoSeccionRepo.ObtenerGradoSeccionPorIdAsync(gradoSeccionId.Value)
                : null;

            var matriculasFiltradas = gradoSeccionId.HasValue
                ? todasMatriculas.Where(m => m.GradoSeccionId == gradoSeccionId.Value && m.IsActive).ToList()
                : todasMatriculas.Where(m => m.IsActive).ToList();

            var estudiantesPorGrado = matriculasFiltradas
                .OrderBy(m => m.GradoSeccion?.Grado)
                .ThenBy(m => m.GradoSeccion?.Seccion)
                .ThenBy(m => m.Student?.Apellido)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(container =>
                    {
                        container.Column(column =>
                        {
                            column.Item().Text("EduConecta Perú").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                            var titulo = gradoSeccion != null
                                ? $"LISTA DE ESTUDIANTES - {gradoSeccion.GradoSeccionNombre}"
                                : "LISTA DE TODOS LOS ESTUDIANTES MATRICULADOS";
                            column.Item().Text(titulo).FontSize(14).Bold();
                            column.Item().Text($"Año Escolar: {DateTime.Now.Year}").FontSize(10);
                            column.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        column.Item().Text($"Total de estudiantes: {estudiantesPorGrado.Count}").FontSize(11).Bold();
                        column.Item().PaddingTop(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(25);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("#").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Estudiante").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("DNI").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Apoderado").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Teléfono").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Grado/Sección").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Año").FontColor(Colors.White).Bold();
                            });

                            int contador = 1;
                            foreach (var matricula in estudiantesPorGrado)
                            {
                                var bgColor = contador % 2 == 0 ? Colors.Grey.Lighten3 : Colors.White;

                                table.Cell().Background(bgColor).Padding(3).Text(contador.ToString());
                                table.Cell().Background(bgColor).Padding(3).Text(matricula.Student?.NombreCompleto ?? "N/A");
                                table.Cell().Background(bgColor).Padding(3).Text(matricula.Student?.DNI ?? "N/A");
                                table.Cell().Background(bgColor).Padding(3).Text(matricula.LegalGuardian?.NombreCompleto ?? "N/A");
                                table.Cell().Background(bgColor).Padding(3).Text(matricula.LegalGuardian?.Telefono ?? "N/A");
                                table.Cell().Background(bgColor).Padding(3).Text(matricula.GradoSeccion?.GradoSeccionNombre ?? "N/A");
                                table.Cell().Background(bgColor).Padding(3).Text(matricula.AnioEscolar.ToString());

                                contador++;
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text($"Página {{p}} de {{pages}}").FontSize(8);
                });
            });

            var pdfBytes = document.GeneratePdf();
            var nombreArchivo = gradoSeccion != null
                ? $"Estudiantes_{gradoSeccion.Grado}_{gradoSeccion.Seccion}_{DateTime.Now:yyyyMMdd}.pdf"
                : $"Estudiantes_Todos_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", nombreArchivo);
        }

        // REPORTE 6: Historial de Pagos por Estudiante
        public async Task<IActionResult> HistorialPagosPorEstudiante(int studentId)
        {
            var estudiante = await _studentRepo.ObtenerEstudiantePorIdAsync(studentId);
            if (estudiante == null)
            {
                return NotFound();
            }

            var todosPagos = await _paymentRepo.ObtenerPagosAsync();
            var pagosEstudiante = todosPagos
                .Where(p => p.StudentId == studentId && p.IsActive)
                .OrderByDescending(p => p.FechaPago)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(container =>
                    {
                        container.Column(column =>
                        {
                            column.Item().Text("EduConecta Perú").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                            column.Item().Text("HISTORIAL DE PAGOS").FontSize(14).Bold();
                            column.Item().PaddingTop(10).Background(Colors.Grey.Lighten3).Padding(10).Column(info =>
                            {
                                info.Item().Text($"Estudiante: {estudiante.NombreCompleto}").FontSize(12).Bold();
                                info.Item().Text($"DNI: {estudiante.DNI}").FontSize(10);
                                if (estudiante.LegalGuardian != null)
                                {
                                    info.Item().Text($"Apoderado: {estudiante.LegalGuardian.NombreCompleto}").FontSize(10);
                                }
                            });
                            column.Item().PaddingTop(5).Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        if (pagosEstudiante.Count == 0)
                        {
                            column.Item().PaddingTop(50).AlignCenter().Text("No hay pagos registrados para este estudiante")
                                .FontSize(12).Italic().FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            column.Item().Text($"Total de pagos realizados: {pagosEstudiante.Count}").FontSize(11).Bold();
                            column.Item().PaddingTop(10);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Green.Darken2).Padding(5).Text("#").FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Green.Darken2).Padding(5).Text("Fecha").FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Green.Darken2).Padding(5).Text("Concepto").FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Green.Darken2).Padding(5).Text("Método").FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Green.Darken2).Padding(5).Text("N° Op.").FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Green.Darken2).Padding(5).AlignRight().Text("Monto").FontColor(Colors.White).Bold();
                                });

                                int contador = 1;
                                foreach (var pago in pagosEstudiante)
                                {
                                    var bgColor = contador % 2 == 0 ? Colors.Grey.Lighten3 : Colors.White;

                                    table.Cell().Background(bgColor).Padding(5).Text(contador.ToString());
                                    table.Cell().Background(bgColor).Padding(5).Text(pago.FechaPago.ToString("dd/MM/yyyy"));
                                    table.Cell().Background(bgColor).Padding(5).Text($"Cuota {pago.Quota?.Mes} {pago.Quota?.Anio}");
                                    table.Cell().Background(bgColor).Padding(5).Text(pago.PaymentType?.TypeName ?? "N/A");
                                    table.Cell().Background(bgColor).Padding(5).Text(pago.NumeroOperacion ?? "-");
                                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"S/ {pago.Monto:N2}").FontColor(Colors.Green.Darken2);

                                    contador++;
                                }

                                table.Cell().ColumnSpan(5).Background(Colors.Green.Darken3).Padding(5).AlignRight().Text("TOTAL PAGADO:").Bold().FontColor(Colors.White);
                                table.Cell().Background(Colors.Green.Darken3).Padding(5).AlignRight().Text($"S/ {pagosEstudiante.Sum(p => p.Monto):N2}").Bold().FontColor(Colors.White);
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text($"Página {{p}} de {{pages}}").FontSize(8);
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Historial_Pagos_{estudiante.DNI}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // REPORTE 7: Resumen Financiero Mensual
        public async Task<IActionResult> ResumenFinancieroMensual(int mes, int anio)
        {
            var todosPagos = await _paymentRepo.ObtenerPagosAsync();
            var pagosMes = todosPagos
                .Where(p => p.IsActive &&
                           p.FechaPago.Month == mes &&
                           p.FechaPago.Year == anio)
                .OrderBy(p => p.FechaPago)
                .ToList();

            var ingresosPorDia = pagosMes
                .GroupBy(p => p.FechaPago.Date)
                .Select(g => new
                {
                    Fecha = g.Key,
                    CantidadPagos = g.Count(),
                    TotalIngresos = g.Sum(p => p.Monto)
                })
                .OrderBy(x => x.Fecha)
                .ToList();

            var ingresosPorMetodo = pagosMes
                .GroupBy(p => p.PaymentType?.TypeName ?? "Sin especificar")
                .Select(g => new
                {
                    Metodo = g.Key,
                    Cantidad = g.Count(),
                    Total = g.Sum(p => p.Monto)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            var nombreMes = new System.Globalization.CultureInfo("es-ES").DateTimeFormat.GetMonthName(mes);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(container =>
                    {
                        container.Column(column =>
                        {
                            column.Item().Text("EduConecta Perú").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                            column.Item().Text("RESUMEN FINANCIERO MENSUAL").FontSize(14).Bold();
                            column.Item().Text($"Período: {nombreMes.ToUpper()} {anio}").FontSize(12).Bold().FontColor(Colors.Green.Darken2);
                            column.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        column.Item().Background(Colors.Green.Lighten4).Padding(15).Column(resumen =>
                        {
                            resumen.Item().Text("RESUMEN GENERAL").FontSize(12).Bold().FontColor(Colors.Green.Darken3);
                            resumen.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Total de Pagos:").FontSize(9);
                                    col.Item().Text(pagosMes.Count.ToString()).FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                                });
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Ingresos Totales:").FontSize(9);
                                    col.Item().Text($"S/ {pagosMes.Sum(p => p.Monto):N2}").FontSize(20).Bold().FontColor(Colors.Green.Darken3);
                                });
                            });
                        });

                        column.Item().PaddingTop(15);

                        column.Item().Text("INGRESOS POR MÉTODO DE PAGO").FontSize(11).Bold();
                        column.Item().PaddingTop(5);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Método").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignCenter().Text("Cantidad").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Total").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignCenter().Text("%").FontColor(Colors.White).Bold();
                            });

                            var totalGeneral = ingresosPorMetodo.Sum(x => x.Total);
                            foreach (var item in ingresosPorMetodo)
                            {
                                var porcentaje = totalGeneral > 0 ? (item.Total / totalGeneral * 100) : 0;

                                table.Cell().Padding(5).Text(item.Metodo);
                                table.Cell().Padding(5).AlignCenter().Text(item.Cantidad.ToString());
                                table.Cell().Padding(5).AlignRight().Text($"S/ {item.Total:N2}");
                                table.Cell().Padding(5).AlignCenter().Text($"{porcentaje:F1}%");
                            }
                        });

                        column.Item().PaddingTop(15);

                        column.Item().Text("INGRESOS DIARIOS").FontSize(11).Bold();
                        column.Item().PaddingTop(5);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Green.Darken2).Padding(5).Text("Fecha").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Green.Darken2).Padding(5).AlignCenter().Text("Pagos").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Green.Darken2).Padding(5).AlignRight().Text("Ingresos").FontColor(Colors.White).Bold();
                            });

                            foreach (var dia in ingresosPorDia)
                            {
                                table.Cell().Padding(5).Text(dia.Fecha.ToString("dd/MM/yyyy - dddd", new System.Globalization.CultureInfo("es-ES")));
                                table.Cell().Padding(5).AlignCenter().Text(dia.CantidadPagos.ToString());
                                table.Cell().Padding(5).AlignRight().Text($"S/ {dia.TotalIngresos:N2}").FontColor(Colors.Green.Darken2);
                            }

                            table.Cell().Background(Colors.Green.Darken3).Padding(5).Text("TOTAL:").Bold().FontColor(Colors.White);
                            table.Cell().Background(Colors.Green.Darken3).Padding(5).AlignCenter().Text(pagosMes.Count.ToString()).Bold().FontColor(Colors.White);
                            table.Cell().Background(Colors.Green.Darken3).Padding(5).AlignRight().Text($"S/ {pagosMes.Sum(p => p.Monto):N2}").Bold().FontColor(Colors.White);
                        });
                    });

                    page.Footer().AlignCenter().Text($"Página {{p}} de {{pages}}").FontSize(8);
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Resumen_Financiero_{nombreMes}_{anio}.pdf");
        }
    }
}