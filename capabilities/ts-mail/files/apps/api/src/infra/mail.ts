import nodemailer from "nodemailer";

const transport = nodemailer.createTransport({
  host: process.env.SMTP_HOST ?? "localhost",
  port: Number(process.env.SMTP_PORT ?? "1025"),
  ignoreTLS: true,
});

export async function sendMail(args: { to: string; subject: string; text?: string; html?: string }) {
  return await transport.sendMail({
    from: process.env.MAIL_FROM ?? "no-reply@{{name}}.local",
    ...args,
  });
}
