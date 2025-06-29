namespace SOFTWARE
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
           ApplicationConfiguration.Initialize();
           Application.SetCompatibleTextRenderingDefault(false);

           Form2 form2 = new Form2();
           form2.Show();
           Application.Run(); // Không truyền form nào vào Run()

        }
    }
}