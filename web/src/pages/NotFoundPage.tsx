import { Link } from "react-router";

export function NotFoundPage() {
  return (
    <div className="py-16 text-center">
      <h1 className="text-2xl font-bold text-slate-900">Page not found</h1>
      <p className="mt-2 text-slate-600">The address is wrong or the page has been removed.</p>
      <Link to="/" className="mt-6 inline-block font-semibold text-brand-700 hover:underline">
        Back to the dashboard
      </Link>
    </div>
  );
}
